using System;
using System.Threading.Tasks;

namespace Bikeapelago.Api.Services;

public interface IArchipelagoService
{
    Task ConnectAsync(Guid sessionId, string url, string slotName, string? password = null);
    Task DisconnectAsync(Guid sessionId);
    Task CheckLocationsAsync(Guid sessionId, long[] locationIds);
    Task SayAsync(Guid sessionId, string message);
}
