using System;
using System.Linq;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bikeapelago.Api.Services;

public interface IItemExecutionService
{
    Task<bool> ExecuteDetourAsync(Guid sessionId, Guid nodeId);
    Task<bool> ExecuteDroneAsync(Guid sessionId, Guid nodeId);
    Task<bool> ExecuteSignalAmplifierAsync(Guid sessionId);
}

public class ItemExecutionService(
    IMapNodeRepository nodeRepository,
    IGameSessionRepository sessionRepository,
    IOsmDiscoveryService osmDiscoveryService,
    IArchipelagoService archipelagoService,
    ILogger<ItemExecutionService> logger) : IItemExecutionService
{
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IOsmDiscoveryService _osmDiscoveryService = osmDiscoveryService;
    private readonly IArchipelagoService _archipelagoService = archipelagoService;
    private readonly ILogger<ItemExecutionService> _logger = logger;

    public async Task<bool> ExecuteDetourAsync(Guid sessionId, Guid nodeId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        var node = await _nodeRepository.GetByIdAsync(nodeId);

        if (session == null || node == null || node.State == "Checked") return false;

        _logger.LogInformation("Executing Detour for node {NodeId} in session {SessionId}", nodeId, sessionId);

        // Fetch replacement point matching the same region tag
        var points = await _osmDiscoveryService.GetRandomNodesAsync(
            session.CenterLat ?? 0,
            session.CenterLon ?? 0,
            session.Radius ?? 5000,
            20,
            session.Mode,
            0.5);

        var replacementPoint = points
            .Where(p => GetRegionTag(CalculateAzimuth(session.CenterLat ?? 0, session.CenterLon ?? 0, p.Lat, p.Lon)) == node.RegionTag)
            .OrderBy(_ => Guid.NewGuid())
            .FirstOrDefault();

        if (replacementPoint != null)
        {
            node.Location = new NetTopologySuite.Geometries.Point(replacementPoint.Lon, replacementPoint.Lat) { SRID = 4326 };
            node.HasBeenRelocated = true;
            await _nodeRepository.UpdateAsync(node);
            return true;
        }

        return false;
    }

    public async Task<bool> ExecuteDroneAsync(Guid sessionId, Guid nodeId)
    {
        var node = await _nodeRepository.GetByIdAsync(nodeId);
        if (node == null || (node.IsArrivalChecked && node.IsPrecisionChecked)) return false;

        _logger.LogInformation("Executing Drone for node {NodeId} in session {SessionId}", nodeId, sessionId);

        node.IsArrivalChecked = true;
        node.IsPrecisionChecked = true;
        node.State = "Checked";
        await _nodeRepository.UpdateAsync(node);

        await _archipelagoService.CheckLocationsAsync(sessionId, [node.ApArrivalLocationId, node.ApPrecisionLocationId]);
        return true;
    }

    public async Task<bool> ExecuteSignalAmplifierAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null) return false;

        _logger.LogInformation("Executing Signal Amplifier for session {SessionId}", sessionId);
        session.SignalAmplifierActive = true;
        await _sessionRepository.UpdateAsync(session);
        return true; 
    }

    private double CalculateAzimuth(double lat1, double lon1, double lat2, double lon2)
    {
        double lat1Rad = lat1 * Math.PI / 180.0;
        double lat2Rad = lat2 * Math.PI / 180.0;
        double dLonRad = (lon2 - lon1) * Math.PI / 180.0;

        double y = Math.Sin(dLonRad) * Math.Cos(lat2Rad);
        double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLonRad);
        double brng = Math.Atan2(y, x);
        return (brng * 180.0 / Math.PI + 360.0) % 360.0;
    }

    private string GetRegionTag(double az)
    {
        if (az >= 315 || az < 45) return "North";
        if (az >= 45 && az < 135) return "East";
        if (az >= 135 && az < 225) return "South";
        return "West";
    }
}
