using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Bikeapelago.Api.Services;

public class GridCacheService : IGridCacheService
{
    private readonly ILogger<GridCacheService> _logger;
    private readonly string _connectionString;

    // Grid cell size: ~0.45 degrees = ~50km
    private const double GRID_CELL_SIZE = 0.45;

    private static bool _tablesEnsured = false;
    private static readonly System.Threading.SemaphoreSlim _lock = new(1, 1);

    public GridCacheService(ILogger<GridCacheService> logger, IConfiguration config)
    {
        _logger = logger;
        _connectionString = config.GetConnectionString("OsmDiscovery")
            ?? throw new InvalidOperationException("OsmDiscovery connection string is required.");
    }

    private async Task EnsureTablesCreatedAsync()
    {
        if (_tablesEnsured) return;

        await _lock.WaitAsync();
        try
        {
            if (_tablesEnsured) return;

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS grid_cache_jobs (
                    id SERIAL PRIMARY KEY,
                    grid_x BIGINT NOT NULL,
                    grid_y BIGINT NOT NULL,
                    mode VARCHAR(32) NOT NULL,
                    status VARCHAR(32) NOT NULL DEFAULT 'pending',
                    data JSONB,
                    error_message TEXT,
                    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP WITH TIME ZONE,
                    started_at TIMESTAMP WITH TIME ZONE,
                    completed_at TIMESTAMP WITH TIME ZONE,
                    UNIQUE (grid_x, grid_y, mode)
                );

                CREATE TABLE IF NOT EXISTS node_grid_cache (
                    grid_x BIGINT NOT NULL,
                    grid_y BIGINT NOT NULL,
                    mode VARCHAR(32) NOT NULL,
                    node_ids BIGINT[] NOT NULL,
                    nodes GEOMETRY(Point, 4326)[] NOT NULL,
                    node_count INTEGER NOT NULL,
                    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                    PRIMARY KEY (grid_x, grid_y, mode)
                );
                """;
            await cmd.ExecuteNonQueryAsync();
            _tablesEnsured = true;
            _logger.LogInformation("Grid cache tables ensured in OsmDiscovery database.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure grid cache tables");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Calculate grid coordinates for a lat/lon point
    /// </summary>
    public (long gridX, long gridY) GetGridCoordinates(double lat, double lon)
    {
        long gridX = (long)Math.Floor(lon / GRID_CELL_SIZE);
        long gridY = (long)Math.Floor(lat / GRID_CELL_SIZE);
        return (gridX, gridY);
    }

    /// <summary>
    /// Get grid cells that cover a radius around a point
    /// </summary>
    public List<(long, long)> GetCoveringGridCells(double lat, double lon, double radiusMeters)
    {
        double radiusDegrees = radiusMeters / 111000.0;
        var (centerX, centerY) = GetGridCoordinates(lat, lon);

        long cellsInRadius = (long)Math.Ceiling(radiusDegrees / GRID_CELL_SIZE) + 1;
        var cells = new List<(long, long)>();

        for (long x = centerX - cellsInRadius; x <= centerX + cellsInRadius; x++)
        {
            for (long y = centerY - cellsInRadius; y <= centerY + cellsInRadius; y++)
            {
                cells.Add((x, y));
            }
        }

        return cells;
    }

    /// <summary>
    /// Check if grid cells are cached for given mode
    /// </summary>
    public async Task<Dictionary<(long, long), bool>> CheckCacheStatusAsync(List<(long, long)> gridCells, string mode)
    {
        if (gridCells.Count == 0)
            return new();

        await EnsureTablesCreatedAsync();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            SELECT DISTINCT c.grid_x, c.grid_y
            FROM node_grid_cache c
            JOIN unnest(@grid_xs, @grid_ys) AS t(x, y)
              ON c.grid_x = t.x AND c.grid_y = t.y
            WHERE c.mode = @mode
            """;

        var gridXs = gridCells.Select(c => c.Item1).ToArray();
        var gridYs = gridCells.Select(c => c.Item2).ToArray();

        cmd.Parameters.AddWithValue("mode", mode);
        cmd.Parameters.Add(new NpgsqlParameter("grid_xs", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Bigint) { Value = gridXs });
        cmd.Parameters.Add(new NpgsqlParameter("grid_ys", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Bigint) { Value = gridYs });

        var cached = new HashSet<(long, long)>();
        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                cached.Add((reader.GetInt64(0), reader.GetInt64(1)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check cache status");
        }

        // Return status for all cells
        return gridCells.ToDictionary(
            cell => cell,
            cell => cached.Contains(cell)
        );
    }

    /// <summary>
    /// Get cached nodes for grid cells
    /// </summary>
    public async Task<List<DiscoveryPoint>> GetCachedNodesAsync(List<(long, long)> gridCells, string mode)
    {
        if (gridCells.Count == 0)
            return [];

        await EnsureTablesCreatedAsync();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 10;

        cmd.CommandText = """
            SELECT DISTINCT ST_X(geom), ST_Y(geom)
            FROM node_grid_cache,
            LATERAL unnest(nodes) AS geom
            WHERE mode = @mode
            AND grid_x = ANY(@grid_xs)
            AND grid_y = ANY(@grid_ys)
            """;

        var gridXs = gridCells.Select(c => c.Item1).Distinct().ToArray();
        var gridYs = gridCells.Select(c => c.Item2).Distinct().ToArray();

        cmd.Parameters.AddWithValue("mode", mode);
        cmd.Parameters.AddWithValue("grid_xs", gridXs);
        cmd.Parameters.AddWithValue("grid_ys", gridYs);

        var results = new List<DiscoveryPoint>();
        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new DiscoveryPoint(reader.GetDouble(0), reader.GetDouble(1)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch cached nodes");
        }

        return results;
    }

    /// <summary>
    /// Queue a cache job for uncached grid cells
    /// </summary>
    public async Task<List<int>> QueueCacheJobsAsync(List<(long, long)> gridCells, string mode)
    {
        var jobIds = new List<int>();

        await EnsureTablesCreatedAsync();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        foreach (var (gridX, gridY) in gridCells)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO grid_cache_jobs (grid_x, grid_y, mode, status)
                VALUES (@grid_x, @grid_y, @mode, 'pending')
                ON CONFLICT DO NOTHING
                RETURNING id
                """;

            cmd.Parameters.AddWithValue("grid_x", gridX);
            cmd.Parameters.AddWithValue("grid_y", gridY);
            cmd.Parameters.AddWithValue("mode", mode);

            try
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && int.TryParse(result.ToString(), out int jobId))
                {
                    jobIds.Add(jobId);
                    _logger.LogInformation("Queued cache job {JobId} for grid cell ({X}, {Y}), mode: {Mode}", jobId, gridX, gridY, mode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to queue cache job for ({X}, {Y})", gridX, gridY);
            }
        }

        return jobIds;
    }

    /// <summary>
    /// Build cache for a grid cell (called by background job)
    /// </summary>
    public async Task BuildCacheForCellAsync(long gridX, long gridY, string mode)
    {
        _logger.LogInformation("Building cache for grid cell ({X}, {Y}), mode: {Mode}", gridX, gridY, mode);

        await EnsureTablesCreatedAsync();

        var jobId = -1;
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Mark as processing
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    UPDATE grid_cache_jobs
                    SET status = 'processing', started_at = NOW()
                    WHERE grid_x = @grid_x AND grid_y = @grid_y AND mode = @mode
                    AND status = 'pending'
                    RETURNING id
                    """;
                cmd.Parameters.AddWithValue("grid_x", gridX);
                cmd.Parameters.AddWithValue("grid_y", gridY);
                cmd.Parameters.AddWithValue("mode", mode);

                var result = await cmd.ExecuteScalarAsync();
                if (result != null) int.TryParse(result.ToString(), out jobId);
            }
            
            if (jobId == -1)
            {
                _logger.LogWarning("No pending job found for grid cell ({X}, {Y}), mode: {Mode}. It may have been picked up by another worker.", gridX, gridY, mode);
                return;
            }

            // Fetch highway tag filters for mode
            string highwayFilter = mode.ToLower() switch
            {
                "walk" => """'footway', 'pedestrian', 'steps', 'corridor', 'path', 'track', 'residential', 'living_street'""",
                _ => """'cycleway', 'residential', 'living_street', 'unclassified', 'tertiary', 'path', 'track'"""
            };

            // Calculate bounding box for grid cell
            double minLon = gridX * GRID_CELL_SIZE;
            double maxLon = (gridX + 1) * GRID_CELL_SIZE;
            double minLat = gridY * GRID_CELL_SIZE;
            double maxLat = (gridY + 1) * GRID_CELL_SIZE;

            // Query node_ids in this grid cell (lightweight - just integers, not geometries)
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandTimeout = 60; // 1 minute (should be fast with just node_ids)
                cmd.CommandText = """
                    INSERT INTO node_grid_cache (grid_x, grid_y, mode, node_ids, nodes, node_count)
                    SELECT
                      @grid_x as grid_x,
                      @grid_y as grid_y,
                      @mode as mode,
                      array_agg(node_id ORDER BY node_id) as node_ids,
                      array_agg(geom ORDER BY node_id) as nodes,
                      count(*) as node_count
                    FROM planet_osm_nodes
                    WHERE ST_X(geom) BETWEEN @min_lon AND @max_lon
                    AND ST_Y(geom) BETWEEN @min_lat AND @max_lat
                    ON CONFLICT (grid_x, grid_y, mode) DO UPDATE SET
                      node_ids = EXCLUDED.node_ids,
                      nodes = EXCLUDED.nodes,
                      node_count = EXCLUDED.node_count,
                      updated_at = NOW()
                    """;

                cmd.Parameters.AddWithValue("grid_x", gridX);
                cmd.Parameters.AddWithValue("grid_y", gridY);
                cmd.Parameters.AddWithValue("mode", mode);
                cmd.Parameters.AddWithValue("min_lon", minLon);
                cmd.Parameters.AddWithValue("max_lon", maxLon);
                cmd.Parameters.AddWithValue("min_lat", minLat);
                cmd.Parameters.AddWithValue("max_lat", maxLat);

                await cmd.ExecuteNonQueryAsync();
            }

            // Mark as completed
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = """
                    UPDATE grid_cache_jobs
                    SET status = 'completed', completed_at = NOW()
                    WHERE id = @id
                    """;
                cmd.Parameters.AddWithValue("id", jobId);
                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("✅ Cache build completed for grid cell ({X}, {Y}), mode: {Mode}", gridX, gridY, mode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to build cache for grid cell ({X}, {Y}), mode: {Mode}", gridX, gridY, mode);

            // Mark as failed
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    UPDATE grid_cache_jobs
                    SET status = 'failed', completed_at = NOW(), error_message = @error
                    WHERE id = @id
                    """;
                cmd.Parameters.AddWithValue("error", ex.Message);
                cmd.Parameters.AddWithValue("id", jobId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { /* Silent fail on error marking */ }
        }
    }
}
