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
    private readonly IMapboxRoutingService _routingService;
    private readonly IGridCacheService _gridCache;

    // Highway tags that are valid for each routing mode.
    // Aligned with common routing profiles (bike, walk, car).
    private static readonly string[] BikeHighwayTags =
    [
        "cycleway", "path", "track", "residential", "unclassified",
        "tertiary", "tertiary_link", "secondary", "secondary_link",
        "primary", "primary_link", "service", "living_street"
    ];

    private static readonly string[] WalkHighwayTags =
    [
        "footway", "path", "pedestrian", "steps", "track", "residential",
        "unclassified", "tertiary", "tertiary_link", "secondary", "secondary_link",
        "primary", "primary_link", "service", "living_street"
    ];

    public PostGisOsmDiscoveryService(ILogger<PostGisOsmDiscoveryService> logger, IConfiguration config, IMapboxRoutingService routingService, IGridCacheService gridCache)
    {
        _logger = logger;
        _connectionString = config.GetConnectionString("OsmDiscovery")
            ?? throw new InvalidOperationException("OsmDiscovery connection string is required.");
        _routingService = routingService;
        _gridCache = gridCache;
    }

    private static string[] HighwayTagsForMode(string mode) =>
        mode.ToLowerInvariant() is "walk" or "foot" ? WalkHighwayTags : BikeHighwayTags;

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

        List<DiscoveryPoint> allNodes;
        var fetchSw = System.Diagnostics.Stopwatch.StartNew();

        // Step 4: Fetch nodes - either from cache (fast) or live query (slow)
        if (uncachedCells.Count == 0 && cachedCells.Count > 0)
        {
            _logger.LogInformation("🚀 Cache HIT: Fetching nodes from {CellCount} grid cells", cachedCells.Count);
            allNodes = await _gridCache.GetCachedNodesAsync(cachedCells, mode);
        }
        else
        {
            // Fallback to live query for any missing coverage
            _logger.LogInformation(
                "⚠️ Cache {Status}: Running live unnest query for {Radius}m radius",
                cachedCells.Count == 0 ? "MISS" : "PARTIAL", radiusMeters);

            // Generate random sub-targets spread across the full radius.
            // Sub-radius scales with parent radius so probes don't overlap on small areas
            // or under-sample on large ones. 2.5x probe count gives buffer for empty probes
            // (rivers, parks, etc.) without dictating connection count.
            double subRadiusMeters = Math.Clamp(radiusMeters * 0.1, 200, 1500);
            int subTargetCount = (int)Math.Ceiling(count * 2.5);

            var subTargets = GenerateRandomPointsInCircle(lat, lon, radiusMeters, subTargetCount, densityBias);
            _logger.LogInformation(
                "Probing {ProbeCount} sub-targets (r={SubRadius}m each)",
                subTargetCount, subRadiusMeters);

            allNodes = await FetchNodesForSubTargetsAsync(subTargets, subRadiusMeters, mode);
        }
        fetchSw.Stop();

        // Step 5: Filter and Sample
        var filteredNodes = allNodes
            .Where(p => CalculateDistance(lat, lon, p.Lat, p.Lon) <= radiusMeters)
            .ToList();

        var shuffled = filteredNodes.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();

        _logger.LogInformation(
            "Discovery returned {Total} unique candidates ({Filtered} inside radius) in {Ms}ms", 
            allNodes.Count, filteredNodes.Count, fetchSw.ElapsedMilliseconds);

        totalSw.Stop();
        _logger.LogInformation("GetRandomNodesAsync total time: {Ms}ms", totalSw.ElapsedMilliseconds);
        return shuffled;
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

    private async Task<List<DiscoveryPoint>> FetchNodesForSubTargetsAsync(
        List<DiscoveryPoint> subTargets, double subRadiusMeters, string mode)
    {
        double[] lons = subTargets.Select(p => p.Lon).ToArray();
        double[] lats = subTargets.Select(p => p.Lat).ToArray();
        
        // 1 degree lat is ~111,132m. Longitude varies by cos(lat).
        // We use a generous degree radius to ensure we don't miss nodes
        // near the edges of the sub-radius due to earth curvature, 
        // then filter precisely in SQL using geography.
        double avgLat = lats.Average();
        double cosLat = Math.Cos(avgLat * Math.PI / 180.0);
        double degreesPerMeter = 1.0 / 111132.0;
        
        // Use the larger of the two degree-equivalent distances (usually longitude) 
        // and add a 50% buffer to be safe against projection distortions.
        double maxDegreesPerMeter = degreesPerMeter / Math.Max(0.1, cosLat);
        double subRadiusDegrees = (subRadiusMeters * 1.5) * maxDegreesPerMeter;
        
        int safetyCap = subTargets.Count * 20;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        // 1. Unnest probes into target geometries.
        // 2. Find ALL valid road lines within a rough degree-radius of ANY probe.
        // 3. For each probe, find the SINGLE NEAREST node that is within the true sub-radius.
        // This prevents the "urban bias" where one probe in a city returns 1000 nodes
        // while one in a rural area returns 5, causing the city to dominate the sample.
        cmd.CommandText = """
            WITH probes AS (
                SELECT ST_SetSRID(ST_MakePoint(lon, lat), 4326) AS probe_geom
                FROM unnest(@lons, @lats) AS t(lon, lat)
            ),
            valid_nodes AS (
                -- For each probe, find nodes of safe roads within the sub-radius
                SELECT 
                    p.probe_geom,
                    (ST_DumpPoints(w.geom)).geom AS node_geom
                FROM probes p
                CROSS JOIN LATERAL (
                    SELECT w.geom
                    FROM planet_osm_ways w
                    WHERE ST_DWithin(w.geom, p.probe_geom, @sub_radius_degrees)
                      AND ((@is_walk AND w.walking_safe) OR (NOT @is_walk AND w.cycling_safe))
                ) w
            ),
            filtered_nodes AS (
                -- Narrow down to nodes actually within the metric sub-radius (using geography for accuracy)
                -- and pick one random node per probe to ensure geographic fairness.
                SELECT DISTINCT ON (probe_geom)
                    ST_X(node_geom)::float8 AS x,
                    ST_Y(node_geom)::float8 AS y
                FROM valid_nodes
                WHERE ST_DWithin(node_geom::geography, probe_geom::geography, @sub_radius_meters)
                ORDER BY probe_geom, RANDOM()
            )
            SELECT x, y FROM filtered_nodes
            LIMIT @safety_cap;
            """;

        bool isWalk = mode.ToLowerInvariant() is "walk" or "foot";
        cmd.Parameters.Add(new NpgsqlParameter("lons", NpgsqlDbType.Array | NpgsqlDbType.Double) { Value = lons });
        cmd.Parameters.Add(new NpgsqlParameter("lats", NpgsqlDbType.Array | NpgsqlDbType.Double) { Value = lats });
        cmd.Parameters.AddWithValue("sub_radius_degrees", subRadiusDegrees);
        cmd.Parameters.AddWithValue("sub_radius_meters", subRadiusMeters);
        cmd.Parameters.AddWithValue("is_walk", isWalk);
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
        
        // WGS84 constants
        const double MetersPerDegreeLat = 111132.0;
        double radLat = centerLat * Math.PI / 180.0;
        double cosLat = Math.Cos(radLat);

        for (int i = 0; i < count; i++)
        {
            // Math.Pow(random, densityBias) controls how probes cluster within the radius:
            //   0.5  = Uniform area distribution (geographically fair, sparse center) — default
            //   0.75 = "Goldilocks" zone (denser center for routing, still spreads to edges)
            //   1.0+ = Linear or exponential distance distribution (heavy center clustering)
            double distance = Math.Pow(Random.Shared.NextDouble(), densityBias) * radiusMeters;
            double angle = Random.Shared.NextDouble() * 2.0 * Math.PI;

            // Simple equirectangular projection is fine for discovery probes
            double latOffset = distance * Math.Cos(angle) / MetersPerDegreeLat;
            double lonOffset = distance * Math.Sin(angle) / (MetersPerDegreeLat * cosLat);

            points.Add(new DiscoveryPoint(centerLon + lonOffset, centerLat + latOffset));
        }

        return points;
    }

    public Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request)
    {
        return _routingService.ValidateNodesAsync(request);
    }
}
