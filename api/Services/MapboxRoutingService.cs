using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using Bikeapelago.Api.Models;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace Bikeapelago.Api.Services;

public class MapboxRoutingService(HttpClient httpClient, ILogger<MapboxRoutingService> logger, IConfiguration configuration, IMemoryCache memoryCache) : IMapboxRoutingService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<MapboxRoutingService> _logger = logger;
    private readonly IMemoryCache _cache = memoryCache;
    private readonly string _mapboxApiKey = configuration["Mapbox:ApiKey"] ?? configuration["MAPBOX_API_KEY"] ?? string.Empty;
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
                var url = $"{MapboxMatchingUrl}/{profile}/{coordinatesQuery}?access_token={Uri.EscapeDataString(_mapboxApiKey)}&geometries=geojson";

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
            var url = $"{MapboxOptimizationUrl}/{profile}/{coordinatesString}?" +
                      $"access_token={Uri.EscapeDataString(_mapboxApiKey)}&" +
                      $"geometries=geojson&" +
                      $"source=first&" +
                      $"destination=last";

            _logger.LogInformation("Calling Mapbox Optimization API with {Count} coordinates for profile {Profile}", coordinates.Count, profile);

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<MapboxOptimizationResponse>(json, opts);

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
        if (userLocation == null)
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
                var tripCoords = trip.GetCoordinates();
                if (tripCoords.Count > 0)
                {
                    allGeometries.AddRange(tripCoords);
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

    /// <summary>
    /// Approximates the positive elevation gain of a route geometry by calling the free Open-Meteo Elevation API.
    /// Safely downsamples routes to a maximum of 100 points to adhere to HTTP GET limits.
    /// Results are cached server-side to prevent rate-limiting from redundant requests.
    /// </summary>
    public async Task<double> CalculateElevationGainAsync(List<List<double>> geometry)
    {
        if (geometry == null || geometry.Count < 2) return 0;

        // Downsample geo coordinates to max 100 evenly spaced nodes
        int maxPoints = 100;
        var sampledPoints = new List<List<double>>();
        int step = Math.Max(1, geometry.Count / maxPoints);

        for (int i = 0; i < geometry.Count; i += step)
        {
            if (sampledPoints.Count < maxPoints)
            {
                sampledPoints.Add(geometry[i]);
            }
        }
        
        // Ensure final point is capped to not lose the route destination
        if (sampledPoints[sampledPoints.Count - 1] != geometry.Last() && sampledPoints.Count < maxPoints)
        {
            sampledPoints.Add(geometry.Last());
        }

        var lats = string.Join(",", sampledPoints.Select(p => Math.Round(p[1], 4).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var lons = string.Join(",", sampledPoints.Select(p => Math.Round(p[0], 4).ToString(System.Globalization.CultureInfo.InvariantCulture)));

        // Use a composite cache key based on the downsampled coordinates string
        string cacheKey = $"elev_gain_{lats.GetHashCode()}_{lons.GetHashCode()}";
        if (_cache.TryGetValue(cacheKey, out double cachedGain))
        {
            _logger.LogInformation("Returning cached elevation gain: {Gain}m", Math.Round(cachedGain));
            return cachedGain;
        }

        var url = $"https://api.open-meteo.com/v1/elevation?latitude={lats}&longitude={lons}";

        try
        {
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Open-Meteo elevation API failed with status {Status}", response.StatusCode);
                return 0;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("elevation", out var elevationArray) && elevationArray.ValueKind == JsonValueKind.Array)
            {
                double totalGain = 0;
                double previousElevation = 0;
                bool isFirst = true;

                foreach (var el in elevationArray.EnumerateArray())
                {
                    double currentElevation = el.GetDouble();
                    if (!isFirst)
                    {
                        double diff = currentElevation - previousElevation;
                        if (diff > 0)
                        {
                            totalGain += diff;
                        }
                    }
                    else
                    {
                        isFirst = false;
                    }
                    previousElevation = currentElevation;
                }

                _logger.LogInformation("Calculated {Gain}m elevation gain from {Count} downsampled points", Math.Round(totalGain), sampledPoints.Count);
                
                // Cache for 1 hour
                _cache.Set(cacheKey, totalGain, TimeSpan.FromHours(1));
                
                return totalGain;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate elevation via Open-Meteo");
        }

        return 0;
    }
}
