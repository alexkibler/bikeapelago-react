using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Bikeapelago.Api.Data;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Services;

/// <summary>
/// Service for querying elevation data from PostGIS raster table (srtm_elevation).
/// Provides both single-point and bulk elevation lookups for routing and analysis.
/// </summary>
public class ElevationService
{
    private readonly BikeapelagoDbContext _context;
    private readonly ILogger<ElevationService> _logger;

    public ElevationService(BikeapelagoDbContext context, ILogger<ElevationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get elevation for a single coordinate (latitude, longitude).
    /// Returns elevation in meters, or null if coordinate is outside raster coverage.
    /// </summary>
    public async Task<int?> GetElevationAsync(double latitude, double longitude)
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"
                    SELECT ROUND(ST_Value(r.rast, ST_SetSRID(ST_Point({longitude}, {latitude}), 4326))::numeric)::int
                    FROM srtm_elevation r
                    WHERE ST_Intersects(r.rast, ST_SetSRID(ST_Point({longitude}, {latitude}), 4326))
                    LIMIT 1
                ";

                var elevation = await command.ExecuteScalarAsync();
                return elevation != null ? (int?)elevation : null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error querying elevation at ({latitude}, {longitude}): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get elevation profile for a route (polyline).
    /// Returns array of (distance, elevation) tuples along the route.
    /// For performance, samples at regular intervals rather than querying every point.
    /// </summary>
    public async Task<List<(double distanceMeters, int? elevationMeters)>> GetElevationProfileAsync(
        List<(double lat, double lon)> routePoints,
        int sampleIntervalMeters = 100)
    {
        if (routePoints == null || routePoints.Count < 2)
            return new();

        var profile = new List<(double, int?)>();
        double cumulativeDistance = 0;

        // Sample every N meters along the route
        for (int i = 0; i < routePoints.Count - 1; i++)
        {
            var (lat1, lon1) = routePoints[i];
            var (lat2, lon2) = routePoints[i + 1];

            // Calculate segment distance in meters (haversine approximation)
            var segmentDistance = CalculateDistanceMeters(lat1, lon1, lat2, lon2);

            // Sample points along this segment
            int samples = Math.Max(1, (int)(segmentDistance / sampleIntervalMeters));
            for (int s = 0; s < samples; s++)
            {
                double fraction = s / (double)samples;
                var sampledLat = lat1 + (lat2 - lat1) * fraction;
                var sampledLon = lon1 + (lon2 - lon1) * fraction;

                var elevation = await GetElevationAsync(sampledLat, sampledLon);
                profile.Add((cumulativeDistance, elevation));

                cumulativeDistance += sampleIntervalMeters;
            }
        }

        return profile;
    }

    /// <summary>
    /// Bulk populate elevation for all MapNodes in a session that don't already have it.
    /// Used to initially populate elevation data after loading raster.
    /// </summary>
    public async Task<(int updated, int skipped, int failed)> PopulateSessionElevationsAsync(Guid sessionId)
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE ""MapNodes"" m
                    SET ""Elevation"" = (
                        SELECT ROUND(ST_Value(r.rast, ST_Transform(m.""Location"", 4326))::numeric)::int
                        FROM srtm_elevation r
                        WHERE ST_Intersects(r.rast, ST_Transform(m.""Location"", 4326))
                        LIMIT 1
                    )
                    WHERE m.""SessionId"" = @sessionId
                      AND m.""Location"" IS NOT NULL
                      AND m.""Elevation"" IS NULL;

                    SELECT ROW_COUNT();
                ";

                var param = command.CreateParameter();
                param.ParameterName = "@sessionId";
                param.Value = sessionId;
                command.Parameters.Add(param);

                var rowsAffected = await command.ExecuteScalarAsync() as int? ?? 0;

                _logger.LogInformation($"Populated elevation for {rowsAffected} nodes in session {sessionId}");

                return (rowsAffected, 0, 0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error populating elevations for session {sessionId}: {ex.Message}");
            return (0, 0, 1);
        }
    }

    /// <summary>
    /// Check if elevation raster is loaded and has data.
    /// </summary>
    public async Task<bool> IsElevationDataAvailableAsync()
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM srtm_elevation;";
                var count = await command.ExecuteScalarAsync();
                return count is int c && c > 0;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get elevation coverage statistics.
    /// </summary>
    public async Task<ElevationStats?> GetElevationStatsAsync()
    {
        try
        {
            var stats = await _context.MapNodes
                .Where(m => m.Location != null)
                .GroupBy(m => 1)
                .Select(g => new
                {
                    TotalNodes = g.Count(),
                    NodesWithElevation = g.Count(m => m.Elevation.HasValue),
                    MinElevation = g.Where(m => m.Elevation.HasValue).Min(m => m.Elevation),
                    MaxElevation = g.Where(m => m.Elevation.HasValue).Max(m => m.Elevation),
                    AvgElevation = (int?)g.Where(m => m.Elevation.HasValue).Average(m => m.Elevation)
                })
                .FirstOrDefaultAsync();

            if (stats == null)
                return null;

            return new ElevationStats
            {
                TotalNodes = stats.TotalNodes,
                NodesWithElevation = stats.NodesWithElevation,
                CoveragePercent = stats.TotalNodes > 0
                    ? (double)stats.NodesWithElevation / stats.TotalNodes * 100
                    : 0,
                MinElevationMeters = stats.MinElevation,
                MaxElevationMeters = stats.MaxElevation,
                AvgElevationMeters = stats.AvgElevation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting elevation stats: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Simple haversine distance calculation in meters.
    /// </summary>
    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        var phi1 = Math.PI / 180 * lat1;
        var phi2 = Math.PI / 180 * lat2;
        var deltaLat = Math.PI / 180 * (lat2 - lat1);
        var deltaLon = Math.PI / 180 * (lon2 - lon1);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}

/// <summary>
/// Statistics about elevation data coverage in the database.
/// </summary>
public class ElevationStats
{
    public int TotalNodes { get; set; }
    public int NodesWithElevation { get; set; }
    public double CoveragePercent { get; set; }
    public int? MinElevationMeters { get; set; }
    public int? MaxElevationMeters { get; set; }
    public int? AvgElevationMeters { get; set; }
}
