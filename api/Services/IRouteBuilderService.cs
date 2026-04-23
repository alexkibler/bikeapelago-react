using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Services;

/// <summary>
/// Orchestrates building an optimized route for a session:
/// resolves the origin point, selects target nodes, calls the routing
/// engine, persists snapped locations, and generates GPX output.
/// </summary>
public interface IRouteBuilderService
{
    /// <summary>
    /// Builds an optimized route for the given session.
    /// </summary>
    /// <param name="sessionId">The session to route within.</param>
    /// <param name="request">Origin, target node IDs, profile, and turn-by-turn flag.</param>
    /// <returns>A <see cref="RouteBuilderResult"/> containing the route data or an error.</returns>
    Task<RouteBuilderResult> BuildRouteAsync(Guid sessionId, RouteWaypointsRequest request);
}

/// <summary>
/// The result of a <see cref="IRouteBuilderService.BuildRouteAsync"/> call.
/// </summary>
public class RouteBuilderResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    // ── Route data (populated on success) ─────────────────────────────────────

    public List<List<double>> Geometry { get; init; } = [];
    public List<Guid> OrderedNodeIds { get; init; } = [];
    public double TotalDistanceMeters { get; init; }
    public double TotalDurationSeconds { get; init; }
    public double ElevationGain { get; init; }
    public string GpxString { get; init; } = string.Empty;

    /// <summary>Snapped road-network positions keyed by node ID string.</summary>
    public Dictionary<string, SnappedNodeLocation> SnappedNodeLocations { get; init; } = [];

    // ── Factory helpers ────────────────────────────────────────────────────────

    public static RouteBuilderResult Fail(string error) =>
        new() { Success = false, Error = error };
}

/// <summary>Serialisable snapped location returned to the frontend.</summary>
public class SnappedNodeLocation
{
    public double Lon { get; init; }
    public double Lat { get; init; }
}
