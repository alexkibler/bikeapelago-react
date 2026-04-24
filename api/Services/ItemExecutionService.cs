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
    int GetActiveInventory(GameSession session, long itemId);
}

public class ItemExecutionService(
    IMapNodeRepository nodeRepository,
    IGameSessionRepository sessionRepository,
    IOsmDiscoveryService osmDiscoveryService,
    IArchipelagoService archipelagoService,
    IProgressionEngineFactory engineFactory,
    ILogger<ItemExecutionService> logger) : IItemExecutionService
{
    private readonly IMapNodeRepository _nodeRepository = nodeRepository;
    private readonly IGameSessionRepository _sessionRepository = sessionRepository;
    private readonly IOsmDiscoveryService _osmDiscoveryService = osmDiscoveryService;
    private readonly IArchipelagoService _archipelagoService = archipelagoService;
    private readonly IProgressionEngineFactory _engineFactory = engineFactory;
    private readonly ILogger<ItemExecutionService> _logger = logger;

    public async Task<bool> ExecuteDetourAsync(Guid sessionId, Guid nodeId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        var node = await _nodeRepository.GetByIdAsync(nodeId);

        if (session == null || node == null || node.SessionId != sessionId || node.State == "Checked") return false;

        _logger.LogInformation("Executing Detour for node {NodeId} ({RegionTag}) in session {SessionId}", nodeId, node.RegionTag, sessionId);

        double totalRadius = session.Radius ?? 5000;
        double hubRadius = totalRadius * 0.25;

        // Fetch candidate replacement points
        var points = await _osmDiscoveryService.GetRandomNodesAsync(
            session.CenterLat ?? 0,
            session.CenterLon ?? 0,
            totalRadius,
            200, // Sample more for better randomness
            session.TransportMode,
            0.5);

        _logger.LogInformation("Found {Count} candidate points for detour. Total Radius: {TotalRadius}, Hub Radius: {HubRadius}", points.Count, totalRadius, hubRadius);

        // Filter for points that are currently "playable" based on session progression
        var playablePoints = points.Where(p => {
            double dist = CalculateDistance(session.CenterLat ?? 0, session.CenterLon ?? 0, p.Lat, p.Lon);
            
            // 1. Hub is always playable
            if (dist <= hubRadius) return true;

            // 2. Beyond hub depends on progression mode
            double az = CalculateAzimuth(session.CenterLat ?? 0, session.CenterLon ?? 0, p.Lat, p.Lon);
            
            if (session.ProgressionMode == "quadrant")
            {
                string tag = GetRegionTag(az);
                if (tag == "North" && session.NorthPassReceived) return true;
                if (tag == "East" && session.EastPassReceived) return true;
                if (tag == "South" && session.SouthPassReceived) return true;
                if (tag == "West" && session.WestPassReceived) return true;
                return false;
            }
            else if (session.ProgressionMode == "radius")
            {
                // steps: 0=25%, 1=50%, 2=75%, 3=100%
                double unlockedRadius = hubRadius * (session.RadiusStep + 1);
                return dist <= unlockedRadius;
            }
            
            // "free" or null progression mode means everything in radius is playable
            return dist <= totalRadius;
        }).ToList();

        _logger.LogInformation("Filtered to {Count} playable candidate points", playablePoints.Count);

        var replacementPoint = playablePoints
            .OrderBy(_ => Guid.NewGuid())
            .FirstOrDefault();

        if (replacementPoint != null)
        {
            // Ensure user has a Detour item after we know the action can succeed.
            if (!await ConsumeItemAsync(session, ItemDefinitions.Detour)) 
            {
                _logger.LogWarning("Attempted to use Detour without any Detour items in session {SessionId}", sessionId);
                return false;
            }

            double dist = CalculateDistance(session.CenterLat ?? 0, session.CenterLon ?? 0, replacementPoint.Lat, replacementPoint.Lon);
            double az = CalculateAzimuth(session.CenterLat ?? 0, session.CenterLon ?? 0, replacementPoint.Lat, replacementPoint.Lon);
            
            string newTag = dist <= hubRadius ? "Hub" : GetRegionTag(az);
            
            _logger.LogInformation("Selected replacement point at {Lat}, {Lon} (New Tag: {Tag})", replacementPoint.Lat, replacementPoint.Lon, newTag);
            
            node.Location = new NetTopologySuite.Geometries.Point(replacementPoint.Lon, replacementPoint.Lat) { SRID = 4326 };
            node.RegionTag = newTag;
            node.State = "Available"; // Since it's in a playable area, it's now Available
            node.HasBeenRelocated = true;
            
            await _nodeRepository.UpdateAsync(node);
            await _archipelagoService.BroadcastMessageAsync(sessionId, $"Used Detour on {node.Name}!", "item");
            return true;
        }

        _logger.LogWarning("Failed to find any playable replacement points among {Count} candidates", points.Count);
        return false;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula
        double r = 6371000; // meters
        double phi1 = lat1 * Math.PI / 180;
        double phi2 = lat2 * Math.PI / 180;
        double dphi = (lat2 - lat1) * Math.PI / 180;
        double dlambda = (lon2 - lon1) * Math.PI / 180;

        double a = Math.Sin(dphi / 2) * Math.Sin(dphi / 2) +
                   Math.Cos(phi1) * Math.Cos(phi2) *
                   Math.Sin(dlambda / 2) * Math.Sin(dlambda / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return r * c;
    }

    public async Task<bool> ExecuteDroneAsync(Guid sessionId, Guid nodeId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        var node = await _nodeRepository.GetByIdAsync(nodeId);
        if (session == null || node == null || node.SessionId != sessionId || (node.IsArrivalChecked && node.IsPrecisionChecked)) return false;

        // Ensure user has a Drone item
        if (!await ConsumeItemAsync(session, ItemDefinitions.Drone))
        {
            _logger.LogWarning("Attempted to use Drone without any Drone items in session {SessionId}", sessionId);
            return false;
        }

        _logger.LogInformation("Executing Drone for node {NodeId} in session {SessionId}", nodeId, sessionId);

        await _archipelagoService.BroadcastMessageAsync(sessionId, $"Drone exploring {node.Name}...", "item");

        var engine = _engineFactory.CreateEngine(session.ConnectionMode);
        await engine.CheckNodesAsync(sessionId, [new NewlyCheckedNode
        {
            Id = node.Id,
            ApArrivalLocationId = node.ApArrivalLocationId,
            ApPrecisionLocationId = node.ApPrecisionLocationId,
            ArrivalChecked = true,
            PrecisionChecked = true,
            Lat = node.Lat ?? 0,
            Lon = node.Lon ?? 0
        }]);

        return true;
    }

    public async Task<bool> ExecuteSignalAmplifierAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null) return false;

        // Ensure user has a Signal Amplifier item
        if (!await ConsumeItemAsync(session, ItemDefinitions.SignalAmplifier))
        {
            _logger.LogWarning("Attempted to use Signal Amplifier without any items in session {SessionId}", sessionId);
            return false;
        }

        _logger.LogInformation("Executing Signal Amplifier for session {SessionId}", sessionId);
        session.SignalAmplifierActive = true;
        await _sessionRepository.UpdateAsync(session);
        await _archipelagoService.BroadcastMessageAsync(sessionId, "Signal Amplifier activated for next ride!", "item");
        return true; 
    }

    public int GetActiveInventory(GameSession session, long itemId)
    {
        int totalReceived = session.ReceivedItemIds.Count(id => id == itemId);
        int totalUsed = 0;

        if (itemId == ItemDefinitions.Detour) totalUsed = session.DetoursUsed;
        else if (itemId == ItemDefinitions.Drone) totalUsed = session.DronesUsed;
        else if (itemId == ItemDefinitions.SignalAmplifier) totalUsed = session.SignalAmplifiersUsed;

        return totalReceived - totalUsed;
    }

    private async Task<bool> ConsumeItemAsync(GameSession session, long itemId)
    {
        int available = GetActiveInventory(session, itemId);
        if (available <= 0)
        {
            return false;
        }

        if (itemId == ItemDefinitions.Detour) session.DetoursUsed++;
        else if (itemId == ItemDefinitions.Drone) session.DronesUsed++;
        else if (itemId == ItemDefinitions.SignalAmplifier) session.SignalAmplifiersUsed++;

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
