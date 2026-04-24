using System;
using System.Threading.Tasks;

namespace Bikeapelago.Api.Services;

public interface IArchipelagoService
{
    Task ConnectAsync(Guid sessionId, string url, string slotName, string? password = null);
    Task DisconnectAsync(Guid sessionId);
    Task CheckLocationsAsync(Guid sessionId, long[] locationIds);
    Task SayAsync(Guid sessionId, string message);
    Task UpdateUnlockedNodesAsync(Guid sessionId, long[] receivedItemIds);
    Task BroadcastMessageAsync(Guid sessionId, string message, string type = "system");
    string GetItemName(Guid sessionId, long itemId);
}
