using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Services;

/// <summary>
/// Service for routing and optimization using Mapbox APIs.
/// Provides validation, snapping, and route optimization for geographic points.
/// </summary>
public interface IMapboxRoutingService
{
    /// <summary>
    /// Validates a set of geographic points against the routing network.
    /// Returns snapped coordinates and distance information for each point.
    /// </summary>
    Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request);

    /// <summary>
    /// Optimizes a route through multiple coordinates using the Mapbox Optimization API.
    /// </summary>
    /// <param name="coordinates">List of coordinates (up to 12)</param>
    /// <param name="profile">Routing profile: "cycling", "driving", or "walking"</param>
    /// <returns>Optimized trip response with geometry and waypoints</returns>
    Task<MapboxOptimizationResponse?> OptimizeRouteAsync(List<MapboxCoordinate> coordinates, string profile = "cycling");

    /// <summary>
    /// Routes through multiple target nodes starting from a user location.
    /// Uses geographic nearest-neighbor sorting and the Mapbox Optimization API.
    /// </summary>
    Task<OptimizedRouteResult> RouteToMultipleNodesAsync(
        NetTopologySuite.Geometries.Point userLocation,
        List<MapNode> targetNodes,
        string profile = "cycling");

    /// <summary>
    /// Calculates the estimated positive elevation gain of a route in meters.
    /// </summary>
    Task<double> CalculateElevationGainAsync(List<List<double>> geometry);
}
