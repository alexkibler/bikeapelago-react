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
using NpgsqlTypes;

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

    public async Task<List<DiscoveryPoint>> GetRandomNodesAsync(double lat, double lon, double radiusMeters, int count, string mode = "bike", double densityBias = 0.5)
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

        // Step 4: Generate random sub-targets spread across the full radius.
        // Sub-radius scales with parent radius so probes don't overlap on small areas
        // or under-sample on large ones. 2.5x probe count gives buffer for empty probes
        // (rivers, parks, etc.) without dictating connection count.
        double subRadiusMeters = Math.Clamp(radiusMeters * 0.1, 200, 1500);
        int subTargetCount = (int)Math.Ceiling(count * 2.5);

        var subTargets = GenerateRandomPointsInCircle(lat, lon, radiusMeters, subTargetCount, densityBias);
        _logger.LogInformation(
            "Running single unnest query with {ProbeCount} sub-targets (r={SubRadius}m each)",
            subTargetCount, subRadiusMeters);

        var fetchSw = System.Diagnostics.Stopwatch.StartNew();
        var allNodes = await FetchNodesForSubTargetsAsync(subTargets, subRadiusMeters);
        fetchSw.Stop();

        var shuffled = allNodes.OrderBy(_ => Random.Shared.Next()).ToList();

        _logger.LogInformation("Unnest query returned {Total} unique candidates in {Ms}ms", shuffled.Count, fetchSw.ElapsedMilliseconds);

        totalSw.Stop();
        _logger.LogInformation("GetRandomNodesAsync total time: {Ms}ms", totalSw.ElapsedMilliseconds);
        return shuffled;
    }

    private async Task<List<DiscoveryPoint>> FetchNodesForSubTargetsAsync(List<DiscoveryPoint> subTargets, double subRadiusMeters)
    {
        double[] lons = subTargets.Select(p => p.Lon).ToArray();
        double[] lats = subTargets.Select(p => p.Lat).ToArray();

        // Convert sub-radius to degrees using the average latitude of the probes.
        // ST_DWithin on geometry uses degree units, so we only need lat-based conversion;
        // the probes are already distributed with correct lon spacing from GenerateRandomPointsInCircle.
        double avgLat = lats.Average();
        double subRadiusDegrees = subRadiusMeters / 111000.0;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 15;
        // Safety cap: prevents Npgsql from streaming an unbounded payload in dense urban areas.
        // ORDER BY RANDOM() is cheap at this stage (applied after DISTINCT, on a small result set).
        int safetyCap = subTargets.Count * 20;

        cmd.CommandText = """
            SELECT x, y FROM (
                SELECT DISTINCT ST_X(n.geom)::float8 AS x, ST_Y(n.geom)::float8 AS y
                FROM planet_osm_nodes n
                JOIN (
                    SELECT ST_SetSRID(ST_MakePoint(lon, lat), 4326) AS target_geom
                    FROM unnest(@lons, @lats) AS t(lon, lat)
                ) sub ON ST_DWithin(n.geom, sub.target_geom, @sub_radius_degrees)
            ) deduped
            ORDER BY RANDOM()
            LIMIT @safety_cap
            """;

        cmd.Parameters.Add(new NpgsqlParameter("lons", NpgsqlDbType.Array | NpgsqlDbType.Double) { Value = lons });
        cmd.Parameters.Add(new NpgsqlParameter("lats", NpgsqlDbType.Array | NpgsqlDbType.Double) { Value = lats });
        cmd.Parameters.AddWithValue("sub_radius_degrees", subRadiusDegrees);
        cmd.Parameters.AddWithValue("safety_cap", safetyCap);

        var results = new List<DiscoveryPoint>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new DiscoveryPoint(reader.GetDouble(0), reader.GetDouble(1)));
        }
        return results;
    }

    internal static List<DiscoveryPoint> GenerateRandomPointsInCircle(double centerLat, double centerLon, double radiusMeters, int count, double densityBias = 0.5)
    {
        var points = new List<DiscoveryPoint>(count);
        double cosLat = Math.Cos(centerLat * Math.PI / 180.0);

        for (int i = 0; i < count; i++)
        {
            // Math.Pow(random, densityBias) controls how probes cluster within the radius:
            //   0.5  = Uniform area distribution (geographically fair, sparse center) — default
            //   0.75 = "Goldilocks" zone (denser center for routing, still spreads to edges)
            //   1.0  = Linear distance distribution (heavy center clustering)
            double distance = Math.Pow(Random.Shared.NextDouble(), densityBias) * radiusMeters;
            double angle = Random.Shared.NextDouble() * 2.0 * Math.PI;

            double latOffset = distance * Math.Cos(angle) / 111000.0;
            double lonOffset = distance * Math.Sin(angle) / (111000.0 * cosLat);

            points.Add(new DiscoveryPoint(centerLon + lonOffset, centerLat + latOffset));
        }

        return points;
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
