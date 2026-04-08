using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Bikeapelago.Api.Repositories;

namespace Bikeapelago.Api.Services;

public record ArchipelagoStatusUpdate(string Status, string? Error = null);
public record ArchipelagoLocationUpdate(long[] LocationIds);
public record ArchipelagoItem(long Id, string Name);
public record ArchipelagoItemsUpdate(ArchipelagoItem[] Items);
public record ArchipelagoChatMessage(string Text, string Type, DateTime Timestamp);

public class ArchipelagoService(IHubContext<ArchipelagoHub> hubContext, ILogger<ArchipelagoService> logger, IServiceScopeFactory scopeFactory)
{
    private readonly ConcurrentDictionary<Guid, IArchipelagoSession> _sessions = new();
    private readonly IHubContext<ArchipelagoHub> _hubContext = hubContext;
    private readonly ILogger<ArchipelagoService> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    private async Task UpdateNodeStatesAsync(Guid sessionId, long[] checkedLocationIds)
    {
        if (checkedLocationIds.Length == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var nodeRepository = scope.ServiceProvider.GetRequiredService<IMapNodeRepository>();
        
        var nodes = await nodeRepository.GetBySessionIdAsync(sessionId);
        var changed = false;

        foreach (var node in nodes)
        {
            if (node.State != "Checked" && checkedLocationIds.Contains(node.ApLocationId))
            {
                _logger.LogTrace("Syncing Node {NodeId} ({Name}) to Checked state from Archipelago Location {LocationId}", node.Id, node.Name, node.ApLocationId);
                node.State = "Checked";
                await nodeRepository.UpdateAsync(node);
                changed = true;
            }
        }

        if (changed)
        {
            _logger.LogInformation("Updated DB for Session {SessionId}: {Count} nodes marked as Checked based on Archipelago sync", sessionId, checkedLocationIds.Length);
        }
    }

    private async Task UpdateUnlockedNodesAsync(Guid sessionId, long[] receivedItemIds)
    {
        if (receivedItemIds.Length == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var nodeRepository = scope.ServiceProvider.GetRequiredService<IMapNodeRepository>();
        
        var nodes = await nodeRepository.GetBySessionIdAsync(sessionId);
        var changed = false;

        foreach (var node in nodes)
        {
            // If the item ID matches the node's ApLocationId, we unlock it
            if (node.State == "Hidden" && receivedItemIds.Contains(node.ApLocationId))
            {
                _logger.LogInformation("Unlocking Node {NodeId} ({Name}) because item {ItemId} was received", node.Id, node.Name, node.ApLocationId);
                node.State = "Available";
                await nodeRepository.UpdateAsync(node);
                changed = true;
            }
        }

        if (changed)
        {
            _logger.LogInformation("Updated DB for Session {SessionId}: Nodes made Available based on items", sessionId);
            await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("OnSyncRequired");
        }
    }

    public async Task ConnectAsync(Guid sessionId, string url, string slotName, string? password = null)
    {
        if (_sessions.TryGetValue(sessionId, out var session) && session.Socket.Connected)
        {
            _logger.LogInformation("Reusing existing Archipelago connection for Session {SessionId}", sessionId);
            var checkedLocations = session.Locations.AllLocationsChecked.ToArray();
            var receivedItems = session.Items.AllItemsReceived.Select(i => i.ItemId).ToArray();

            _ = BroadcastStatus(sessionId, "connected");
            _ = BroadcastLocations(sessionId, checkedLocations);
            _ = BroadcastItems(sessionId, receivedItems);
            _ = UpdateNodeStatesAsync(sessionId, checkedLocations);
            _ = UpdateUnlockedNodesAsync(sessionId, receivedItems);
            return;
        }

        _logger.LogInformation("Connecting Session {SessionId} to Archipelago at {Url} as {Slot}", sessionId, url, slotName);

        if (_sessions.TryRemove(sessionId, out var existingSession))
        {
            try { await existingSession.Socket.DisconnectAsync(); } catch { }
        }

        _logger.LogInformation("Connecting Session {SessionId} to Archipelago server at {Url} as {Slot}", sessionId, url, slotName);
        session = ArchipelagoSessionFactory.CreateSession(url);
        
        session.Items.ItemReceived += (helper) => {
            var allReceivedItems = session.Items.AllItemsReceived.Select(i => i.ItemId).ToArray();
            _ = UpdateUnlockedNodesAsync(sessionId, allReceivedItems);
            _ = SaveItemsToDbAsync(sessionId, allReceivedItems);
            _ = BroadcastItems(sessionId, allReceivedItems);
            
            if (helper.Any()) {
                var item = helper.DequeueItem();
                var itemName = session.Items.GetItemName(item.ItemId) ?? item.ItemId.ToString();
                var player = session.Players.GetPlayerAlias(item.Player) ?? "Unknown";
                var text = $"Received {itemName} from {player}";
                _ = BroadcastMessage(sessionId, text, "item");
            }
        };

        session.Locations.CheckedLocationsUpdated += (locations) => {
            var locationArray = locations.ToArray();
            _ = BroadcastLocations(sessionId, locationArray);
            _ = UpdateNodeStatesAsync(sessionId, locationArray);
        };

        session.Socket.PacketReceived += (packet) => {
             if (packet is PrintJsonPacket printJson) {
                 var text = string.Join("", printJson.Data.Select(d => d.Text));
                 _ = BroadcastMessage(sessionId, text, "system");
             }
        };

        session.Socket.ErrorReceived += (ex, message) => {
            _logger.LogError(ex, "Archipelago Socket Error: {Message}", message);
            _ = BroadcastStatus(sessionId, "error", message);
        };

        LoginResult result;
        try {
            result = session.TryConnectAndLogin("Bikeapelago", slotName, ItemsHandlingFlags.AllItems, password: password);
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to connect to Archipelago socket.");
            await BroadcastStatus(sessionId, "error", "Failed to connect to host: " + ex.Message);
            return;
        }

        if (result.Successful)
        {
            _sessions[sessionId] = session;
            var checkedLocations = session.Locations.AllLocationsChecked.ToArray();
            var receivedItems = session.Items.AllItemsReceived.Select(i => i.ItemId).ToArray();
            
            _ = BroadcastStatus(sessionId, "connected");
            _ = BroadcastLocations(sessionId, checkedLocations);
            _ = BroadcastItems(sessionId, receivedItems);
            _ = UpdateNodeStatesAsync(sessionId, checkedLocations);
            _ = UpdateUnlockedNodesAsync(sessionId, receivedItems);
            _ = SaveItemsToDbAsync(sessionId, receivedItems);
            _ = BroadcastMessage(sessionId, $"Connected to Archipelago as {slotName}", "system");
        }
        else
        {
            var loginFailure = (LoginFailure)result;
            var error = string.Join(", ", loginFailure.Errors);
            _logger.LogWarning("Archipelago Login Failed: {Error}", error);
            _ = BroadcastStatus(sessionId, "error", error);
        }
    }

    public async Task CheckLocationsAsync(Guid sessionId, long[] locationIds)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            _logger.LogInformation("Checking locations {LocationIds} for Session {SessionId}", string.Join(", ", locationIds), sessionId);
            await session.Locations.CompleteLocationChecksAsync(locationIds);
        }
        else
        {
            _logger.LogWarning("Cannot check locations for Session {SessionId}: No active Archipelago connection found", sessionId);
        }
    }

    public async Task DisconnectAsync(Guid sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            await session.Socket.DisconnectAsync();
            _ = BroadcastStatus(sessionId, "disconnected");
        }
    }

    public async Task SayAsync(Guid sessionId, string message)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            await session.Socket.SendPacketAsync(new SayPacket { Text = message });
        }
    }

    private async Task BroadcastStatus(Guid sessionId, string status, string? error = null)
    {
        await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("OnStatusUpdate", new ArchipelagoStatusUpdate(status, error));
    }

    private async Task BroadcastLocations(Guid sessionId, long[] locationIds)
    {
        await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("OnLocationsUpdate", new ArchipelagoLocationUpdate(locationIds));
    }

    private async Task BroadcastMessage(Guid sessionId, string text, string type)
    {
        await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("OnChatMessage", new ArchipelagoChatMessage(text, type, DateTime.UtcNow));
    }

    private async Task BroadcastItems(Guid sessionId, long[] itemIds)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return;
        
        var items = itemIds.Select(id => new ArchipelagoItem(id, session.Items.GetItemName(id) ?? $"Item {id}")).ToArray();
        await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("OnItemsUpdate", new ArchipelagoItemsUpdate(items));
    }

    private async Task SaveItemsToDbAsync(Guid sessionId, long[] itemIds)
    {
        try {
            using var scope = _scopeFactory.CreateScope();
            var sessionRepository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
            var session = await sessionRepository.GetByIdAsync(sessionId);
            if (session != null) {
                session.ReceivedItemIds = itemIds.ToList();
                await sessionRepository.UpdateAsync(session);
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to save items to DB for session {SessionId}", sessionId);
        }
    }
}
