using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using Bikeapelago.Api.Models;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace Bikeapelago.Api.Services;

public class MapboxRoutingService(
    HttpClient httpClient,
    ILogger<MapboxRoutingService> logger,
    IConfiguration configuration,
    IMemoryCache memoryCache,
    IGeographicSortingService geographicSortingService) : IMapboxRoutingService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<MapboxRoutingService> _logger = logger;
    private readonly IMemoryCache _cache = memoryCache;
    private readonly IGeographicSortingService _geographicSortingService = geographicSortingService;
    private readonly string _mapboxApiKey = configuration["Mapbox:ApiKey"] ?? configuration["MAPBOX_API_KEY"] ?? string.Empty;
    private const double MaxDistanceMeters = 20;
    private const string MapboxMatchingUrl = "https://api.mapbox.com/matching/v5/mapbox";
    private const string MapboxOptimizationUrl = "https://api.mapbox.com/optimized-trips/v1/mapbox";
    private const string OsrmBaseUrl = "http://router.project-osrm.org";
    private const int MaxCoordinates = 12;
    private static readonly int[] RetryDelaysMs = [1000, 2000, 4000];

    public async Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request)
    {
        _logger.LogInformation("Validating {Count} nodes for profile {Profile}", request.Points.Length, request.Profile);

        var osrmResults = await RetryAsync(() => TryOsrmValidateAsync(request), "OSRM validation");
        if (osrmResults != null)
            return osrmResults;

        _logger.LogInformation("OSRM validation exhausted retries, falling back to Mapbox");
        var mapboxResults = await RetryAsync(() => TryMapboxValidateAsync(request), "Mapbox validation");
        return mapboxResults ?? await MapboxValidateNodesAsync(request);
    }

    private async Task<List<ValidateResult>?> TryOsrmValidateAsync(ValidateRequest request)
    {
        var profile = MapToOsrmProfile(request.Profile);
        var results = new List<ValidateResult>();

        try
        {
            foreach (var point in request.Points)
            {
                var url = $"{OsrmBaseUrl}/nearest/v1/{profile}/{point.Lon},{point.Lat}?number=1";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OSRM nearest returned {StatusCode} for point {Lat},{Lon}", response.StatusCode, point.Lat, point.Lon);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("code", out var codeEl) || codeEl.GetString() != "Ok")
                {
                    _logger.LogWarning("OSRM nearest returned non-Ok code for point {Lat},{Lon}", point.Lat, point.Lon);
                    return null;
                }

                if (root.TryGetProperty("waypoints", out var waypoints) && waypoints.GetArrayLength() > 0)
                {
                    var wp = waypoints[0];
                    var location = wp.GetProperty("location");
                    var snappedLon = location[0].GetDouble();
                    var snappedLat = location[1].GetDouble();
                    var distance = wp.GetProperty("distance").GetDouble();
                    var roadName = wp.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";

                    results.Add(new ValidateResult(
                        Original: point,
                        Snapped: new DiscoveryPoint(snappedLon, snappedLat),
                        DistanceMeters: distance,
                        IsValid: distance <= MaxDistanceMeters,
                        RoadName: roadName
                    ));
                }
                else
                {
                    results.Add(new ValidateResult(Original: point, IsValid: false, Error: "No nearest road found via OSRM"));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSRM validation failed");
            return null;
        }
    }

    private async Task<List<ValidateResult>> MapboxValidateNodesAsync(ValidateRequest request)
    {
        var results = new List<ValidateResult>();

        foreach (var point in request.Points)
        {
            try
            {
                var profile = request.Profile.ToLower() switch
                {
                    "foot" or "walk" => "walking",
                    "car" => "driving",
                    _ => "cycling"
                };

                var coordinatesQuery = $"{point.Lon},{point.Lat}";
                var url = $"{MapboxMatchingUrl}/{profile}/{coordinatesQuery}?access_token={Uri.EscapeDataString(_mapboxApiKey)}&geometries=geojson";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("matchings", out var matchings) && matchings.GetArrayLength() > 0)
                    {
                        var match = matchings[0];

                        if (match.TryGetProperty("geometry", out var geometry) &&
                            geometry.TryGetProperty("coordinates", out var coordinates) &&
                            coordinates.GetArrayLength() > 0)
                        {
                            var snappedCoord = coordinates[0];
                            var snappedLon = snappedCoord[0].GetDouble();
                            var snappedLat = snappedCoord[1].GetDouble();
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

    private async Task<List<ValidateResult>?> TryMapboxValidateAsync(ValidateRequest request)
    {
        try
        {
            return await MapboxValidateNodesAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mapbox validation attempt failed");
            return null;
        }
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
                      $"destination=last&" +
                      $"roundtrip=false";

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
            var sortedNodes = _geographicSortingService.SortByNearestNeighbor(userLocation, targetNodes);
            _logger.LogInformation("Sorted {Count} nodes by nearest neighbor", sortedNodes.Count);

            // Step 2: Break into chunks of 11 nodes (+ 1 starting location = 12 per API call)
            const int NodesPerChunk = 11;
            var chunks = ChunkNodes(sortedNodes, NodesPerChunk);
            _logger.LogInformation("Split into {ChunkCount} chunks", chunks.Count);

            // Step 3: Process each chunk, chaining them together
            var allGeometries = new List<List<double>>();
            var allNodeIds = new List<Guid>();
            var allSnappedLocations = new Dictionary<Guid, List<double>>();
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

                // Try OSRM first with retry, fall back to Mapbox with retry
                var response = await RetryAsync(() => TryOsrmOptimizeAsync(coordinates, profile), $"OSRM optimization chunk {i + 1}");
                if (response == null)
                {
                    _logger.LogInformation("OSRM optimization exhausted retries for chunk {ChunkNum}, falling back to Mapbox", i + 1);
                    response = await RetryAsync(() => TryMapboxOptimizeAsync(coordinates, profile), $"Mapbox optimization chunk {i + 1}");
                }

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

                // Map waypoints back to node IDs and capture snapped locations.
                // Waypoints are returned in input order; index 0 is the starting location.
                for (int w = 1; w < response.Waypoints.Count && w - 1 < chunks[i].Count; w++)
                {
                    var node = chunks[i][w - 1];
                    allNodeIds.Add(node.Id);

                    var wp = response.Waypoints[w];
                    if (wp.Location.Count >= 2)
                        allSnappedLocations[node.Id] = wp.Location; // [lon, lat]
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
                SnappedLocations = allSnappedLocations,
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

    private async Task<T?> RetryAsync<T>(Func<Task<T?>> operation, string operationName) where T : class
    {
        var result = await operation();
        if (result != null) return result;

        foreach (var delayMs in RetryDelaysMs)
        {
            _logger.LogInformation("{Operation} failed, retrying in {Delay}ms", operationName, delayMs);
            await Task.Delay(delayMs);
            result = await operation();
            if (result != null) return result;
        }

        _logger.LogWarning("{Operation} failed after all retries", operationName);
        return null;
    }

    private async Task<MapboxOptimizationResponse?> TryMapboxOptimizeAsync(List<MapboxCoordinate> coordinates, string profile)
    {
        try
        {
            var result = await OptimizeRouteAsync(coordinates, profile);
            return result?.Code == "Ok" && result.Trips.Count > 0 ? result : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mapbox optimization attempt failed");
            return null;
        }
    }

    private static string MapToOsrmProfile(string profile) => profile.ToLower() switch
    {
        "foot" or "walk" => "walking",
        "car" => "driving",
        _ => "cycling"
    };

    private async Task<MapboxOptimizationResponse?> TryOsrmOptimizeAsync(List<MapboxCoordinate> coordinates, string profile)
    {
        try
        {
            var osrmProfile = MapToOsrmProfile(profile);
            var coordinatesString = string.Join(";", coordinates.Select(c => $"{c.Longitude},{c.Latitude}"));
            var url = $"{OsrmBaseUrl}/trip/v1/{osrmProfile}/{coordinatesString}?source=first&destination=last&roundtrip=false&geometries=geojson";

            _logger.LogInformation("Trying OSRM trip API with {Count} coordinates for profile {Profile}", coordinates.Count, osrmProfile);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OSRM trip API returned {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<MapboxOptimizationResponse>(json, opts);

            if (result?.Code != "Ok" || result.Trips.Count == 0)
            {
                _logger.LogWarning("OSRM trip API returned code {Code}: {Message}", result?.Code, result?.Message);
                return null;
            }

            _logger.LogInformation("OSRM trip success: {TripCount} trips, {WaypointCount} waypoints", result.Trips.Count, result.Waypoints.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSRM trip API call failed");
            return null;
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

    /// <inheritdoc />
    /// <inheritdoc />
    public string GenerateGpx(List<List<double>> geometry, List<MapNode> orderedNodes, bool turnByTurn, Dictionary<Guid, List<double>>? snappedLocations = null)
    {
        // If we have no waypoints and no geometry, there is nothing to generate.
        if (orderedNodes.Count == 0 && geometry.Count == 0)
            throw new InvalidOperationException("Cannot generate GPX with no waypoints or geometry.");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<gpx version=\"1.1\" creator=\"Bikeapelago\" xmlns=\"http://www.topografix.com/GPX/1/1\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd\">");

        // 1. Add Waypoints (<wpt>) for Garmin landmarks/stops
        foreach (var node in orderedNodes)
        {
            if (node.Location == null) continue;

            double lat, lon;
            if (snappedLocations != null && snappedLocations.TryGetValue(node.Id, out var snapped) && snapped.Count >= 2)
            {
                lon = snapped[0];
                lat = snapped[1];
            }
            else
            {
                lon = node.Location.X;
                lat = node.Location.Y;
            }

            sb.AppendLine($"  <wpt lat=\"{lat}\" lon=\"{lon}\">");
            sb.AppendLine($"    <name>{System.Security.SecurityElement.Escape(node.Name)}</name>");
            sb.AppendLine($"    <sym>Waypoint</sym>");
            sb.AppendLine("  </wpt>");
        }

        // 2. Add the actual route or track geometry
        if (turnByTurn && geometry.Count > 0)
        {
            sb.AppendLine("  <trk>");
            sb.AppendLine("    <name>Bikeapelago Route (Turn-by-Turn)</name>");
            sb.AppendLine("    <trkseg>");
            foreach (var point in geometry)
            {
                // geometry points are [lon, lat]
                sb.AppendLine($"      <trkpt lat=\"{point[1]}\" lon=\"{point[0]}\" />");
            }
            sb.AppendLine("    </trkseg>");
            sb.AppendLine("  </trk>");
        }
        else if (!turnByTurn && orderedNodes.Count > 0)
        {
            sb.AppendLine("  <rte>");
            sb.AppendLine("    <name>Bikeapelago Destinations (Straight Line)</name>");
            foreach (var node in orderedNodes)
            {
                if (node.Location == null) continue;

                double lat, lon;
                if (snappedLocations != null && snappedLocations.TryGetValue(node.Id, out var snapped) && snapped.Count >= 2)
                {
                    lon = snapped[0];
                    lat = snapped[1];
                }
                else
                {
                    lon = node.Location.X;
                    lat = node.Location.Y;
                }

                sb.AppendLine($"    <rtept lat=\"{lat}\" lon=\"{lon}\">");
                sb.AppendLine($"      <name>{System.Security.SecurityElement.Escape(node.Name)}</name>");
                sb.AppendLine("    </rtept>");
            }
            sb.AppendLine("  </rte>");
        }

        sb.AppendLine("</gpx>");
        return sb.ToString();
    }
}
