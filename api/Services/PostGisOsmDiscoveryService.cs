using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Bikeapelago.Api.Services;

public class PostGisOsmDiscoveryService : IOsmDiscoveryService
{
    private readonly ILogger<PostGisOsmDiscoveryService> _logger;
    private readonly string _connectionString;
    private readonly HttpClient _httpClient;
    private readonly GridCacheService _gridCache;

    public PostGisOsmDiscoveryService(ILogger<PostGisOsmDiscoveryService> logger, IConfiguration config, HttpClient httpClient, GridCacheService gridCache)
    {
        _logger = logger;
        _connectionString = config.GetConnectionString("OsmDiscovery")
            ?? throw new InvalidOperationException("OsmDiscovery connection string is required.");
        _httpClient = httpClient;
        _gridCache = gridCache;
    }

    public async Task<List<DiscoveryPoint>> GetRandomNodesAsync(double lat, double lon, double radiusMeters, int count, string mode = "bike")
    {
        _logger.LogInformation("Fetching random nodes from PostGIS at {Lat},{Lon} radius {Radius}m, mode: {Mode}", lat, lon, radiusMeters, mode);
        var totalSw = System.Diagnostics.Stopwatch.StartNew();

        // Step 1: Get grid cells covering this radius
        var gridCells = _gridCache.GetCoveringGridCells(lat, lon, radiusMeters);
        _logger.LogInformation("Covering {CellCount} grid cells for radius {Radius}m", gridCells.Count, radiusMeters);

        // Step 2: Check cache status
        var cacheStatus = await _gridCache.CheckCacheStatusAsync(gridCells, mode);
        var cachedCells = cacheStatus.Where(x => x.Value).Select(x => x.Key).ToList();
        var uncachedCells = cacheStatus.Where(x => !x.Value).Select(x => x.Key).ToList();

        _logger.LogInformation("Cache status: {CachedCount} cached, {UncachedCount} uncached", cachedCells.Count, uncachedCells.Count);

        // Step 3: Queue cache jobs for uncached cells (fire-and-forget)
        if (uncachedCells.Count > 0)
        {
            var jobIds = await _gridCache.QueueCacheJobsAsync(uncachedCells, mode);
            _logger.LogInformation("Queued {JobCount} cache jobs: {JobIds}", jobIds.Count, string.Join(", ", jobIds));
        }

        // Step 4: Fetch nodes using direct query (works immediately, doesn't need cache)
        // Once cache is populated, we can optimize to use it instead
        await using var conn = new NpgsqlConnection(_connectionString);
        var connSw = System.Diagnostics.Stopwatch.StartNew();
        await conn.OpenAsync();
        connSw.Stop();
        _logger.LogInformation("Connection opened in {Ms}ms", connSw.ElapsedMilliseconds);

        int fetchCount = Math.Max(count * 3, 100);
        var fetchSw = System.Diagnostics.Stopwatch.StartNew();
        var nodes = await FetchNodesInRadiusAsync(conn, lat, lon, radiusMeters, fetchCount);
        fetchSw.Stop();
        _logger.LogInformation("PostGIS fetch returned {Count} nodes in {Ms}ms", nodes.Count, fetchSw.ElapsedMilliseconds);

        if (nodes.Count == 0)
            return [];

        // Step 5: Randomize and return
        var randSw = System.Diagnostics.Stopwatch.StartNew();
        var random = nodes.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();
        randSw.Stop();
        _logger.LogInformation("Randomized and selected {Count} nodes in {Ms}ms", random.Count, randSw.ElapsedMilliseconds);

        totalSw.Stop();
        _logger.LogInformation("GetRandomNodesAsync total time: {Ms}ms (grid cache jobs queued for next request)", totalSw.ElapsedMilliseconds);
        return random;
    }

    private async Task<List<DiscoveryPoint>> FetchNodesInRadiusAsync(NpgsqlConnection conn, double lat, double lon, double radiusMeters, int count)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 10;

        // Use native geometry type to leverage GIST index
        // Convert meters to degrees: 1 degree ≈ 111km
        double radiusDegrees = radiusMeters / 111000.0;

        cmd.CommandText = """
            SELECT ST_X(geom)::float8, ST_Y(geom)::float8
            FROM planet_osm_nodes
            WHERE ST_DWithin(
                geom,
                ST_SetSRID(ST_MakePoint(@lon, @lat), 4326),
                @radius_degrees
            )
            LIMIT @count
            """;

        cmd.Parameters.AddWithValue("lon", lon);
        cmd.Parameters.AddWithValue("lat", lat);
        cmd.Parameters.AddWithValue("radius_degrees", radiusDegrees);
        cmd.Parameters.AddWithValue("count", count);

        var results = new List<DiscoveryPoint>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new DiscoveryPoint(reader.GetDouble(0), reader.GetDouble(1)));
        }
        return results;
    }

    public async Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request)
    {
        _logger.LogInformation("Validating {Count} nodes via GraphHopper for profile {Profile}", request.Points.Length, request.Profile);

        var results = new List<ValidateResult>();
        const double maxDistanceMeters = 20;

        foreach (var point in request.Points)
        {
            try
            {
                var vehicle = request.Profile.ToLower() switch
                {
                    "foot" or "walk" => "foot",
                    "car" => "car",
                    _ => "bike"
                };
                
                // We use the proxied GraphHopper URL or the one from config
                // For simplicity, let's assume it's available via localhost/graphhopper if internal
                var url = $"http://graphhopper:8989/nearest?point={point.Lat},{point.Lon}&vehicle={vehicle}&type=json";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    var coords = root.GetProperty("coordinates");
                    var snappedLon = coords[0].GetDouble();
                    var snappedLat = coords[1].GetDouble();
                    var distanceFromGraphHopper = root.GetProperty("distance").GetDouble();

                    results.Add(new ValidateResult(
                        Original: point,
                        Snapped: new DiscoveryPoint(snappedLon, snappedLat),
                        DistanceMeters: distanceFromGraphHopper,
                        IsValid: distanceFromGraphHopper <= maxDistanceMeters,
                        RoadName: $"Valid {request.Profile} route"
                    ));
                }
                else
                {
                    results.Add(new ValidateResult(Original: point, IsValid: false, Error: "GraphHopper lookup failed"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate point {Lat},{Lon}", point.Lat, point.Lon);
                results.Add(new ValidateResult(Original: point, IsValid: false, Error: ex.Message));
            }
        }

        return results;
    }
}
