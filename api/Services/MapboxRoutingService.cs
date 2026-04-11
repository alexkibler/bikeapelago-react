using System.Text.Json;
using Bikeapelago.Api.Models;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace Bikeapelago.Api.Services;

public class MapboxRoutingService(HttpClient httpClient, ILogger<MapboxRoutingService> logger, IConfiguration configuration) : IMapboxRoutingService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<MapboxRoutingService> _logger = logger;
    private readonly string _mapboxApiKey = configuration["MAPBOX_API_KEY"] ?? throw new InvalidOperationException("MAPBOX_API_KEY is required.");
    private const double MaxDistanceMeters = 20;
    private const string MapboxMatchingUrl = "https://api.mapbox.com/matching/v5/mapbox";
    private const string MapboxOptimizationUrl = "https://api.mapbox.com/optimized-trips/v1/mapbox";
    private const int MaxCoordinates = 12;

    public async Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request)
    {
        _logger.LogInformation("Validating {Count} nodes via Mapbox for profile {Profile}", request.Points.Length, request.Profile);

        var results = new List<ValidateResult>();

        foreach (var point in request.Points)
        {
            try
            {
                // Use Mapbox Match Service to snap point to nearest road
                var profile = request.Profile.ToLower() switch
                {
                    "foot" or "walk" => "walking",
                    "car" => "driving",
                    _ => "cycling"
                };

                // Build Mapbox coordinates query: lon,lat (not lat,lon!)
                var coordinatesQuery = $"{point.Lon},{point.Lat}";
                var url = $"{MapboxMatchingUrl}/{profile}/v5/{coordinatesQuery}?access_token={Uri.EscapeDataString(_mapboxApiKey)}&geometries=geojson";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    // Check if there are matches
                    if (root.TryGetProperty("matchings", out var matchings) && matchings.GetArrayLength() > 0)
                    {
                        // Get the best match (index 0)
                        var match = matchings[0];

                        if (match.TryGetProperty("geometry", out var geometry) &&
                            geometry.TryGetProperty("coordinates", out var coordinates) &&
                            coordinates.GetArrayLength() > 0)
                        {
                            // Get snapped coordinates (first coordinate pair)
                            var snappedCoord = coordinates[0];
                            var snappedLon = snappedCoord[0].GetDouble();
                            var snappedLat = snappedCoord[1].GetDouble();

                            // Calculate distance between original and snapped point
                            var distanceMeters = GeoDistanceMeters(point.Lat, point.Lon, snappedLat, snappedLon);

                            results.Add(new ValidateResult(
                                Original: point,
                                Snapped: new DiscoveryPoint(snappedLon, snappedLat),
                                DistanceMeters: distanceMeters,
                                IsValid: distanceMeters <= MaxDistanceMeters,
                                RoadName: $"Valid {request.Profile} route"
                            ));
                        }
                        else
                        {
                            results.Add(new ValidateResult(Original: point, IsValid: false, Error: "No snapped coordinates in Mapbox response"));
                        }
                    }
                    else
                    {
                        results.Add(new ValidateResult(Original: point, IsValid: false, Error: "No matching road found in Mapbox"));
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Mapbox API returned {StatusCode}: {Error}", response.StatusCode, errorContent);
                    results.Add(new ValidateResult(Original: point, IsValid: false, Error: $"Mapbox lookup failed: {response.StatusCode}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate point {Lat},{Lon} with Mapbox", point.Lat, point.Lon);
                results.Add(new ValidateResult(Original: point, IsValid: false, Error: ex.Message));
            }
        }

        return results;
    }

    /// <summary>
    /// Calls the Mapbox Optimization API to find the optimal route through multiple coordinates.
    /// Uses a greedy optimization algorithm to minimize total distance/time.
    /// </summary>
    /// <param name="coordinates">List of coordinates (up to 12)</param>
    /// <param name="profile">Routing profile: "cycling", "driving", or "walking" (default: cycling)</param>
    /// <returns>Optimized trip with waypoints and geometry</returns>
    public async Task<MapboxOptimizationResponse?> OptimizeRouteAsync(List<MapboxCoordinate> coordinates, string profile = "cycling")
    {
        if (coordinates == null || coordinates.Count == 0)
            throw new ArgumentException("At least one coordinate is required.");

        if (coordinates.Count > MaxCoordinates)
            throw new ArgumentException($"Maximum {MaxCoordinates} coordinates allowed, got {coordinates.Count}");

        try
        {
            // Format coordinates as: lon,lat;lon,lat;...
            var coordinatesString = string.Join(";", coordinates.Select(c => $"{c.Longitude},{c.Latitude}"));

            // Build URL: source=first&destination=last ensures start and end points are fixed
            var url = $"{MapboxOptimizationUrl}/{profile}/v1/{coordinatesString}?" +
                      $"access_token={Uri.EscapeDataString(_mapboxApiKey)}&" +
                      $"source=first&" +
                      $"destination=last";

            _logger.LogInformation("Calling Mapbox Optimization API with {Count} coordinates for profile {Profile}", coordinates.Count, profile);

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MapboxOptimizationResponse>(json);

                if (result?.Code == "Ok")
                {
                    _logger.LogInformation("Optimization successful: {TripCount} trips, {WaypointCount} waypoints",
                        result.Trips.Count, result.Waypoints.Count);
                    return result;
                }
                else
                {
                    _logger.LogWarning("Optimization failed with code {Code}: {Message}", result?.Code, result?.Message);
                    return result;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Mapbox Optimization API returned {StatusCode}: {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Mapbox Optimization API with {Count} coordinates", coordinates.Count);
            throw;
        }
    }

    /// <summary>
    /// Routes through multiple target nodes starting from a user location.
    /// Uses geographic nearest-neighbor sorting and the Mapbox Optimization API
    /// to find an efficient visit order.
    ///
    /// For large node lists, automatically chunks into groups of 12 coordinates
    /// (1 starting location + 11 nodes) and chains the results together.
    /// </summary>
    /// <param name="userLocation">Starting location (user's current position)</param>
    /// <param name="targetNodes">Unsorted list of nodes to visit</param>
    /// <param name="profile">Routing profile: "cycling", "driving", or "walking"</param>
    /// <returns>Optimized route with ordered geometry and node IDs</returns>
    public async Task<OptimizedRouteResult> RouteToMultipleNodesAsync(
        Point userLocation,
        List<MapNode> targetNodes,
        string profile = "cycling")
    {
        if (userLocation?.Y == null || userLocation.X == null)
            return new OptimizedRouteResult { Success = false, Error = "Invalid user location" };

        if (targetNodes == null || targetNodes.Count == 0)
            return new OptimizedRouteResult { Success = false, Error = "No target nodes provided" };

        try
        {
            _logger.LogInformation("Routing to {Count} nodes from ({Lat},{Lon})", targetNodes.Count, userLocation.Y, userLocation.X);

            // Step 1: Sort nodes by geographic proximity using Nearest Neighbor
            var sortedNodes = GeographicSortingService.SortByNearestNeighbor(userLocation, targetNodes);
            _logger.LogInformation("Sorted {Count} nodes by nearest neighbor", sortedNodes.Count);

            // Step 2: Break into chunks of 11 nodes (+ 1 starting location = 12 per API call)
            const int NodesPerChunk = 11;
            var chunks = ChunkNodes(sortedNodes, NodesPerChunk);
            _logger.LogInformation("Split into {ChunkCount} chunks", chunks.Count);

            // Step 3: Process each chunk, chaining them together
            var allGeometries = new List<List<double>>();
            var allNodeIds = new List<Guid>();
            var currentLocation = userLocation;
            double totalDistance = 0;
            double totalDuration = 0;

            for (int i = 0; i < chunks.Count; i++)
            {
                _logger.LogInformation("Processing chunk {ChunkNum}/{Total} with {NodeCount} nodes", i + 1, chunks.Count, chunks[i].Count);

                // Build coordinate list: starting location + chunk nodes
                var coordinates = new List<MapboxCoordinate>
                {
                    new MapboxCoordinate(currentLocation.X, currentLocation.Y)
                };
                coordinates.AddRange(chunks[i].Select(n => new MapboxCoordinate(n.Location!.X, n.Location.Y)));

                // Call Mapbox Optimization API
                var response = await OptimizeRouteAsync(coordinates, profile);

                if (response?.Code != "Ok" || response.Trips.Count == 0)
                {
                    return new OptimizedRouteResult
                    {
                        Success = false,
                        Error = $"Chunk {i + 1} optimization failed: {response?.Message ?? "No trips returned"}"
                    };
                }

                var trip = response.Trips[0];

                // Collect geometry from this trip
                if (trip.Geometry?.Coordinates.Count > 0)
                {
                    allGeometries.AddRange(trip.Geometry.Coordinates);
                }

                // Map waypoints back to node IDs (skip the first waypoint which is the starting location)
                for (int w = 1; w < response.Waypoints.Count && w - 1 < chunks[i].Count; w++)
                {
                    allNodeIds.Add(chunks[i][w - 1].Id);
                }

                // Update current location to the last snapped waypoint
                if (response.Waypoints.Count > 0)
                {
                    var lastWaypoint = response.Waypoints[response.Waypoints.Count - 1];
                    currentLocation = new NetTopologySuite.Geometries.Point(lastWaypoint.Longitude, lastWaypoint.Latitude) { SRID = 4326 };
                }

                totalDistance += trip.Distance;
                totalDuration += trip.Duration;

                _logger.LogInformation("Chunk {ChunkNum} done: {Distance}m, {Duration}s", i + 1, trip.Distance, trip.Duration);
            }

            return new OptimizedRouteResult
            {
                Success = true,
                Geometry = allGeometries,
                OrderedNodeIds = allNodeIds,
                TotalDistanceMeters = totalDistance,
                TotalDurationSeconds = totalDuration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing to multiple nodes");
            return new OptimizedRouteResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Splits a list of nodes into chunks of the specified size.
    /// </summary>
    private static List<List<MapNode>> ChunkNodes(List<MapNode> nodes, int chunkSize)
    {
        var chunks = new List<List<MapNode>>();
        for (int i = 0; i < nodes.Count; i += chunkSize)
        {
            chunks.Add(nodes.Skip(i).Take(chunkSize).ToList());
        }
        return chunks;
    }

    /// <summary>
    /// Calculate distance between two lat/lon points in meters using Haversine formula.
    /// </summary>
    private static double GeoDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusMeters = 6371000;

        var lat1Rad = lat1 * Math.PI / 180;
        var lat2Rad = lat2 * Math.PI / 180;
        var deltaLat = (lat2 - lat1) * Math.PI / 180;
        var deltaLon = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }
}
