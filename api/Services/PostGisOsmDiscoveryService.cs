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

    public PostGisOsmDiscoveryService(ILogger<PostGisOsmDiscoveryService> logger, IConfiguration config, IMapboxRoutingService routingService)
    {
        _logger = logger;
        _connectionString = config.GetConnectionString("PostGis")
            ?? throw new InvalidOperationException("PostGis connection string is required.");
        _routingService = routingService;
    }

    private static string[] HighwayTagsForMode(string mode) =>
        mode.ToLowerInvariant() is "walk" or "foot" ? WalkHighwayTags : BikeHighwayTags;

    public async Task<List<DiscoveryPoint>> GetRandomNodesAsync(double lat, double lon, double radiusMeters, int count, string mode = "bike", double densityBias = 0.5, string gameMode = "singleplayer")
    {
        _logger.LogInformation("Fetching random nodes from PostGIS at {Lat},{Lon} radius {Radius}m, mode: {Mode}, gameMode: {GameMode}", lat, lon, radiusMeters, mode, gameMode);
        var totalSw = System.Diagnostics.Stopwatch.StartNew();

        // Generate random sub-targets spread across the full radius.
        // Archipelago mode uses 5× oversampling because AP seeds have a hard location
        // count requirement — running short on candidates would break the seed.
        // Singleplayer uses the lighter 2.5× buffer since a retry is harmless.
        double oversampleMultiplier = gameMode.Equals("archipelago", StringComparison.OrdinalIgnoreCase) ? 5.0 : 2.5;
        double subRadiusMeters = Math.Clamp(radiusMeters * 0.1, 200, 1500);
        int subTargetCount = (int)Math.Ceiling(count * oversampleMultiplier);

        var subTargets = GenerateRandomPointsInCircle(lat, lon, radiusMeters, subTargetCount, densityBias);
        _logger.LogInformation(
            "Running single unnest query with {ProbeCount} sub-targets (r={SubRadius}m each, {Multiplier}x oversampling)",
            subTargetCount, subRadiusMeters, oversampleMultiplier);

        var fetchSw = System.Diagnostics.Stopwatch.StartNew();
        var allNodes = await FetchNodesForSubTargetsAsync(subTargets, subRadiusMeters, lat, lon, radiusMeters, mode);
        fetchSw.Stop();

        var shuffled = allNodes.OrderBy(_ => Random.Shared.Next()).ToList();

        _logger.LogInformation("Unnest query returned {Total} unique candidates in {Ms}ms", shuffled.Count, fetchSw.ElapsedMilliseconds);

        totalSw.Stop();
        _logger.LogInformation("GetRandomNodesAsync total time: {Ms}ms", totalSw.ElapsedMilliseconds);
        return shuffled;
    }

    private async Task<List<DiscoveryPoint>> FetchNodesForSubTargetsAsync(
        List<DiscoveryPoint> subTargets, double subRadiusMeters,
        double centerLat, double centerLon, double sessionRadiusMeters, string mode)
    {
        double[] lons = subTargets.Select(p => p.Lon).ToArray();
        double[] lats = subTargets.Select(p => p.Lat).ToArray();
        double subRadiusDegrees = subRadiusMeters / 111000.0;
        double sessionRadiusDegrees = sessionRadiusMeters / 111000.0;
        int safetyCap = subTargets.Count * 20;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        // Extract physical nodes directly from valid lines, removing the need for
        // the obsolete planet_osm_way_nodes relational table.
        // Filter uses the pre-computed cycling_safe/walking_safe boolean columns
        // stored by the Lua flex script — no tags hstore needed.
        // LATERAL join forces the planner to use the GIST index per sub-target.
        // Final ST_DWithin on @center clips any nodes whose OSM way extended outside
        // the session ring boundary.
        cmd.CommandText = """
            WITH center AS (
                SELECT ST_SetSRID(ST_MakePoint(@center_lon, @center_lat), 4326) AS geom
            ),
            sub_targets AS (
                -- Unnest the C# arrays into target geometries (SRID 4326)
                SELECT ST_SetSRID(ST_MakePoint(lon, lat), 4326) AS target_geom
                FROM unnest(@lons, @lats) AS t(lon, lat)
            ),
            valid_lines AS (
                -- LATERAL forces index-nested-loop: one GIST lookup per sub-target
                SELECT DISTINCT w.geom
                FROM sub_targets st
                CROSS JOIN LATERAL (
                    SELECT w.geom
                    FROM planet_osm_ways w
                    WHERE ST_DWithin(w.geom, st.target_geom, @sub_radius_degrees)
                      AND ((@is_walk AND w.walking_safe) OR (NOT @is_walk AND w.cycling_safe))
                ) w
            ),
            dumped_points AS (
                -- Extract the physical nodes (vertices) out of those valid lines
                SELECT (ST_DumpPoints(geom)).geom AS pt_geom
                FROM valid_lines
            )
            -- Deduplicate, clip to session ring, randomize, and cap
            SELECT x, y FROM (
                SELECT DISTINCT
                    ST_X(pt_geom)::float8 AS x,
                    ST_Y(pt_geom)::float8 AS y
                FROM dumped_points, center
                WHERE ST_DWithin(pt_geom, center.geom, @session_radius_degrees)
            ) deduped
            ORDER BY RANDOM()
            LIMIT @safety_cap;
            """;

        bool isWalk = mode.ToLowerInvariant() is "walk" or "foot";
        cmd.Parameters.Add(new NpgsqlParameter("lons", NpgsqlDbType.Array | NpgsqlDbType.Double) { Value = lons });
        cmd.Parameters.Add(new NpgsqlParameter("lats", NpgsqlDbType.Array | NpgsqlDbType.Double) { Value = lats });
        cmd.Parameters.AddWithValue("center_lat", centerLat);
        cmd.Parameters.AddWithValue("center_lon", centerLon);
        cmd.Parameters.AddWithValue("sub_radius_degrees", subRadiusDegrees);
        cmd.Parameters.AddWithValue("session_radius_degrees", sessionRadiusDegrees);
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

    public Task<List<ValidateResult>> ValidateNodesAsync(ValidateRequest request)
    {
        return _routingService.ValidateNodesAsync(request);
    }
}
