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
        return await GetRandomNodesInWedgeAsync(lat, lon, radiusMeters, 0, 360, count, mode, densityBias);
    }

    public async Task<List<DiscoveryPoint>> GetRandomNodesInWedgeAsync(double lat, double lon, double radiusMeters, double startDeg, double endDeg, int count, string mode = "bike", double densityBias = 0.5)
    {
        _logger.LogInformation("Fetching random nodes from PostGIS at {Lat},{Lon} radius {Radius}m, wedge {Start}-{End}, mode: {Mode}", lat, lon, radiusMeters, startDeg, endDeg, mode);
        var totalSw = System.Diagnostics.Stopwatch.StartNew();

        // Step 1: Get grid cells covering this radius
        var gridCells = _gridCache.GetCoveringGridCells(lat, lon, radiusMeters);
        
        // Step 2: Filter cells that are at least partially in the wedge to save cache probes
        var wedgeCells = gridCells.Where(c => {
             // For simplicity, just check the cell corners
             // In a perfect world we'd check if the cell polygon intersects the wedge
             return true; 
        }).ToList();

        // Step 3: Check cache status
        var cacheStatus = await _gridCache.CheckCacheStatusAsync(wedgeCells, mode);
        var cachedCells = cacheStatus.Where(x => x.Value).Select(x => x.Key).ToList();
        var uncachedCells = cacheStatus.Where(x => !x.Value).Select(x => x.Key).ToList();

        if (uncachedCells.Count > 0)
        {
            await _gridCache.QueueCacheJobsAsync(uncachedCells, mode);
        }

        List<DiscoveryPoint> allNodes;
        if (uncachedCells.Count == 0 && cachedCells.Count > 0)
        {
            allNodes = await _gridCache.GetCachedNodesAsync(cachedCells, mode);
        }
        else
        {
            // Probing specifically within the wedge
            double subRadiusMeters = Math.Clamp(radiusMeters * 0.1, 200, 1500);
            int subTargetCount = (int)Math.Ceiling(count * 2.5);

            var subTargets = GenerateRandomPointsInWedge(lat, lon, radiusMeters, startDeg, endDeg, subTargetCount, densityBias);
            allNodes = await FetchNodesForSubTargetsAsync(subTargets, subRadiusMeters, mode);
        }

        // Step 5: Precise Filter and Sample
        var filteredNodes = allNodes
            .Where(p => {
                double dist = CalculateDistance(lat, lon, p.Lat, p.Lon);
                if (dist > radiusMeters) return false;
                double az = CalculateAzimuth(lat, lon, p.Lat, p.Lon);
                return IsInWedge(az, startDeg, endDeg);
            })
            .ToList();

        var shuffled = filteredNodes.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();

        totalSw.Stop();
        _logger.LogInformation("GetRandomNodesInWedgeAsync total time: {Ms}ms, found {Count} nodes", totalSw.ElapsedMilliseconds, shuffled.Count);
        return shuffled;
    }

    private static double CalculateAzimuth(double lat1, double lon1, double lat2, double lon2)
    {
        double lat1Rad = lat1 * Math.PI / 180.0;
        double lat2Rad = lat2 * Math.PI / 180.0;
        double dLonRad = (lon2 - lon1) * Math.PI / 180.0;

        double y = Math.Sin(dLonRad) * Math.Cos(lat2Rad);
        double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLonRad);
        double brng = Math.Atan2(y, x);
        return (brng * 180.0 / Math.PI + 360.0) % 360.0;
    }

    private static bool IsInWedge(double az, double start, double end)
    {
        if (Math.Abs(start - 0) < 0.01 && Math.Abs(end - 360) < 0.01) return true;
        if (start < end) return az >= start && az <= end;
        return az >= start || az <= end;
    }

    internal static List<DiscoveryPoint> GenerateRandomPointsInWedge(double centerLat, double centerLon, double radiusMeters, double startDeg, double endDeg, int count, double densityBias = 0.5)
    {
        var points = new List<DiscoveryPoint>(count);
        const double MetersPerDegreeLat = 111132.0;
        double radLat = centerLat * Math.PI / 180.0;
        double cosLat = Math.Cos(radLat);

        double startRad = (startDeg % 360) * Math.PI / 180.0;
        double endRad = (endDeg % 360) * Math.PI / 180.0;
        
        if (endRad <= startRad) endRad += 2.0 * Math.PI;
        double diffRad = endRad - startRad;

        for (int i = 0; i < count; i++)
        {
            double distance = Math.Pow(Random.Shared.NextDouble(), densityBias) * radiusMeters;
            // Angle in radians, relative to North (0)
            double angleOffset = Random.Shared.NextDouble() * diffRad;
            double angle = (startRad + angleOffset);

            // Note: angle here is standard compass bearing (0=North, clockwise)
            // But standard Math.Cos/Sin use 0=East, counter-clockwise.
            // Converting compass angle to math angle: MathAngle = 90 - CompassAngle
            double mathAngle = (Math.PI / 2.0) - angle;

            double latOffset = distance * Math.Sin(mathAngle) / MetersPerDegreeLat;
            double lonOffset = distance * Math.Cos(mathAngle) / (MetersPerDegreeLat * cosLat);

            points.Add(new DiscoveryPoint(centerLon + lonOffset, centerLat + latOffset));
        }

        return points;
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
