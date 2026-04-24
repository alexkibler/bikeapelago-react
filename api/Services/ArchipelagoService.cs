using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Bikeapelago.Api.Data;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bikeapelago.Api.Services;

public class ArchipelagoService : IArchipelagoService
{
    private readonly ILogger<ArchipelagoService> _logger;
    private readonly IHubContext<ArchipelagoHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<Guid, IArchipelagoSession> _sessions = new();
    private readonly ConcurrentDictionary<Guid, Task> _pendingItemUpdates = new();

    public ArchipelagoService(
        ILogger<ArchipelagoService> logger,
        IHubContext<ArchipelagoHub> hubContext,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
    }

    private async Task UpdateNodeStatesAsync(Guid sessionId, long[] checkedLocationIds)
    {
        if (checkedLocationIds.Length == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var nodeRepository = scope.ServiceProvider.GetRequiredService<IMapNodeRepository>();

        var nodes = await nodeRepository.GetBySessionIdAsync(sessionId);
        var changed = false;

        var nodesToUpdate = new List<MapNode>();
        foreach (var node in nodes)
        {
            var arrivalChecked = checkedLocationIds.Contains(node.ApArrivalLocationId);
            var precisionChecked = checkedLocationIds.Contains(node.ApPrecisionLocationId);
            
            bool nodeChanged = false;
            if (arrivalChecked && !node.IsArrivalChecked)
            {
                node.IsArrivalChecked = true;
                nodeChanged = true;
            }
            if (precisionChecked && !node.IsPrecisionChecked)
            {
                node.IsPrecisionChecked = true;
                nodeChanged = true;
            }

            if (nodeChanged)
            {
                if (node.IsArrivalChecked && node.IsPrecisionChecked)
                {
                    node.State = "Checked";
                }
                
                nodesToUpdate.Add(node);
                changed = true;
            }
        }

        if (changed)
        {
            await nodeRepository.UpdateRangeAsync(nodesToUpdate);
            _logger.LogInformation("Updated DB for Session {SessionId}: {Count} nodes updated based on Archipelago sync", sessionId, nodesToUpdate.Count);
        }
    }

    public async Task UpdateUnlockedNodesAsync(Guid sessionId, long[] receivedItemIds)
    {
        if (receivedItemIds.Length == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var nodeRepository = scope.ServiceProvider.GetRequiredService<IMapNodeRepository>();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();

        var session = await sessionRepository.GetByIdAsync(sessionId);
        if (session == null) return;

        var nodes = await nodeRepository.GetBySessionIdAsync(sessionId);
        var changed = false;

        bool sessionChanged = false;
        if (!session.NorthPassReceived && receivedItemIds.Contains(ItemDefinitions.NorthPass)) { session.NorthPassReceived = true; sessionChanged = true; }
        if (!session.EastPassReceived && receivedItemIds.Contains(ItemDefinitions.EastPass)) { session.EastPassReceived = true; sessionChanged = true; }
        if (!session.SouthPassReceived && receivedItemIds.Contains(ItemDefinitions.SouthPass)) { session.SouthPassReceived = true; sessionChanged = true; }
        if (!session.WestPassReceived && receivedItemIds.Contains(ItemDefinitions.WestPass)) { session.WestPassReceived = true; sessionChanged = true; }
        
        int radiusIncreases = receivedItemIds.Count(id => id == ItemDefinitions.ProgressiveRadiusIncrease);
        if (radiusIncreases > session.RadiusStep) { session.RadiusStep = radiusIncreases; sessionChanged = true; }

        if (sessionChanged) await sessionRepository.UpdateAsync(session);

        var nodesToUpdate = new List<MapNode>();
        foreach (var node in nodes)
        {
            if (node.State == "Hidden" && IsNodeUnlocked(session, node, receivedItemIds))
            {
                _logger.LogInformation("Unlocking Node {NodeId} ({Name}) because progression requirements met", node.Id, node.Name);
                node.State = "Available";
                nodesToUpdate.Add(node);
                changed = true;
            }
        }

        if (changed)
        {
            await nodeRepository.UpdateRangeAsync(nodesToUpdate);
            await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("OnSyncRequired");
        }
    }

    public async Task BroadcastMessageAsync(Guid sessionId, string message, string type = "system") =>
        await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("OnChatMessage", message, type);

    public string GetItemName(Guid sessionId, long itemId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return session.Items.GetItemName(itemId) ?? itemId.ToString();
        }

        return ItemDefinitions.GetItemName(itemId);
    }

    private bool IsItemNamed(Guid sessionId, long itemId, string name)
    {
        return GetItemName(sessionId, itemId) == name;
    }

    private bool IsNodeUnlocked(GameSession session, MapNode node, long[] receivedItemIds)
    {
        if (node.RegionTag == "Hub") return true;

        if (session.ProgressionMode == "quadrant")
        {
            return node.RegionTag switch
            {
                "North" => session.NorthPassReceived,
                "East" => session.EastPassReceived,
                "South" => session.SouthPassReceived,
                "West" => session.WestPassReceived,
                _ => true
            };
        }
        else if (session.ProgressionMode == "radius")
        {
            if (node.Lat == null || node.Lon == null || session.CenterLat == null || session.CenterLon == null) return true;
            
            double dist = CalculateDistance(session.CenterLat.Value, session.CenterLon.Value, node.Lat.Value, node.Lon.Value);
            double maxRadius = session.Radius ?? 5000;
            
            double allowedRadius = (session.RadiusStep) switch
            {
                0 => maxRadius * 0.25,
                1 => maxRadius * 0.50,
                2 => maxRadius * 0.75,
                3 => maxRadius,
                _ => maxRadius
            };
            
            return dist <= allowedRadius;
        }
        else if (session.ProgressionMode == "free")
        {
            return receivedItemIds.Any(id => GetItemName(session.Id, id) == $"{node.Name} Reveal");
        }

        return true;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double r = 6371000;
        double phi1 = lat1 * Math.PI / 180;
        double phi2 = lat2 * Math.PI / 180;
        double dphi = (lat2 - lat1) * Math.PI / 180;
        double dlambda = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dphi / 2) * Math.Sin(dphi / 2) + Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(dlambda / 2) * Math.Sin(dlambda / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
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

        if (_sessions.TryRemove(sessionId, out var existingSession))
        {
            try { await existingSession.Socket.DisconnectAsync(); } catch { }
        }

        session = ArchipelagoSessionFactory.CreateSession(url);

        session.Items.ItemReceived += (helper) =>
        {
            _pendingItemUpdates.AddOrUpdate(sessionId,
                _ => Task.Delay(250).ContinueWith(_ => ProcessItemUpdateAsync(sessionId)),
                (id, existingTask) => existingTask.IsCompleted || existingTask.IsFaulted || existingTask.IsCanceled
                    ? Task.Delay(250).ContinueWith(_ => ProcessItemUpdateAsync(sessionId))
                    : existingTask
            );
            
            bool isInitialSync = !_sessions.ContainsKey(sessionId);
            while (helper.Any())
            {
                var item = helper.DequeueItem();
                if (!isInitialSync)
                {
                    var itemName = session.Items.GetItemName(item.ItemId) ?? item.ItemId.ToString();
                    var player = session.Players.GetPlayerAlias(item.Player) ?? "Unknown";
                    _ = BroadcastMessage(sessionId, $"Received {itemName} from {player}", "item");
                }
            }
        };

        session.Locations.CheckedLocationsUpdated += (locations) =>
        {
            var locationArray = locations.ToArray();
            _ = BroadcastLocations(sessionId, locationArray);
            _ = UpdateNodeStatesAsync(sessionId, locationArray);
        };

        LoginResult result;
        try
        {
            result = await Task.Run(() => session.TryConnectAndLogin("Bikeapelago", slotName, ItemsHandlingFlags.AllItems, password: password));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Archipelago socket.");
            await BroadcastStatus(sessionId, "error", "Failed to connect to host: " + ex.Message);
            return;
        }

        if (result.Successful)
        {
            var loginResult = (LoginSuccessful)result;
            _sessions[sessionId] = session;
            var checkedLocations = session.Locations.AllLocationsChecked.ToArray();
            var receivedItems = session.Items.AllItemsReceived.Select(i => i.ItemId).ToArray();

            await SyncSlotDataAsync(sessionId, loginResult);

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
            _ = BroadcastStatus(sessionId, "error", error);
        }
    }

    private async Task SyncSlotDataAsync(Guid sessionId, LoginSuccessful loginResult)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var session = await sessionRepository.GetByIdAsync(sessionId);
        
        if (session == null) return;

        bool changed = false;
        if (loginResult.SlotData.TryGetValue("progression_mode", out var modeObj))
        {
            int modeInt = Convert.ToInt32(modeObj);
            string modeStr = modeInt switch {
                0 => "quadrant",
                1 => "radius",
                2 => "free",
                _ => "None"
            };
            
            if (session.ProgressionMode != modeStr)
            {
                _logger.LogInformation("Updating Session {SessionId} ProgressionMode to {Mode} from Archipelago SlotData", sessionId, modeStr);
                session.ProgressionMode = modeStr;
                changed = true;
            }
        }

        if (changed) await sessionRepository.UpdateAsync(session);
    }

    private async Task ProcessItemUpdateAsync(Guid sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            var allReceivedItems = session.Items.AllItemsReceived.Select(i => i.ItemId).ToArray();
            await UpdateUnlockedNodesAsync(sessionId, allReceivedItems);
            await SaveItemsToDbAsync(sessionId, allReceivedItems);
            await BroadcastItems(sessionId, allReceivedItems);
        }
    }

    private async Task SaveItemsToDbAsync(Guid sessionId, long[] receivedItemIds)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        await sessionRepository.UpdateReceivedItemsAsync(sessionId, receivedItemIds.ToList());
    }

    private async Task BroadcastStatus(Guid sessionId, string status, string? error = null) =>
        await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("OnStatusChanged", status, error);

    private async Task BroadcastLocations(Guid sessionId, long[] checkedLocations) =>
        await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("OnLocationsChecked", checkedLocations);

    private async Task BroadcastItems(Guid sessionId, long[] receivedItems) =>
        await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("OnItemsReceived", receivedItems);

    private async Task BroadcastMessage(Guid sessionId, string message, string type) =>
        await _hubContext.Clients.Group(sessionId.ToString()).SendAsync("OnChatMessage", message, type);

    public async Task CheckLocationsAsync(Guid sessionId, long[] locationIds)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            await session.Locations.CompleteLocationChecksAsync(locationIds);
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
}
