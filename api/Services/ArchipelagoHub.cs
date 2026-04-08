using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Bikeapelago.Api.Services;

public class ArchipelagoHub(ArchipelagoService archipelagoService, ILogger<ArchipelagoHub> logger) : Hub
{
    private readonly ArchipelagoService _archipelagoService = archipelagoService;
    private readonly ILogger<ArchipelagoHub> _logger = logger;

    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        _logger.LogInformation("Client {ConnectionId} joined session {SessionId}", Context.ConnectionId, sessionId);
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        _logger.LogInformation("Client {ConnectionId} left session {SessionId}", Context.ConnectionId, sessionId);
    }

    public async Task ConnectToArchipelago(string sessionId, string url, string slotName, string? password)
    {
        if (Guid.TryParse(sessionId, out var sessionGuid))
        {
            await _archipelagoService.ConnectAsync(sessionGuid, url, slotName, password);
        }
        else
        {
            _logger.LogWarning("Invalid SessionId format: {SessionId}", sessionId);
        }
    }

    public async Task SendMessage(string sessionId, string message)
    {
        if (Guid.TryParse(sessionId, out var sessionGuid))
        {
            await _archipelagoService.SayAsync(sessionGuid, message);
        }
    }

    public async Task Disconnect(string sessionId)
    {
        if (Guid.TryParse(sessionId, out var sessionGuid))
        {
            await _archipelagoService.DisconnectAsync(sessionGuid);
        }
    }
}
