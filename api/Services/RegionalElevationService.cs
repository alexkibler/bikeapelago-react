using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Bikeapelago.Api.Data;

namespace Bikeapelago.Api.Services;

/// <summary>
/// Manages regional elevation data with lazy loading via grid cache jobs.
/// Dynamically sizes cache regions based on session radius and chunks large downloads.
///
/// Region Grid: 1°×1° SRTM tiles (~111km × 85-111km depending on latitude)
/// Example: Region "N40W080" covers 40-41°N, 80-81°W
///
/// Chunking Strategy:
/// - Small sessions (≤10 tiles): Download all at once
/// - Large sessions (>10 tiles): Break into configurable batches with exponential backoff
/// </summary>
public class RegionalElevationService
{
    // Configuration constants - tune these for optimal downloading
    private const int TILES_BEFORE_CHUNKING = 10;           // Don't chunk unless >10 tiles needed
    private const int TILES_PER_CHUNK = 5;                   // Download in batches of N tiles
    private const int INITIAL_RETRY_DELAY_MS = 2000;         // Start with 2 second delay on rate limit
    private const int MAX_RETRY_DELAY_MS = 300000;           // Max 5 minutes between retries
    private const double BACKOFF_MULTIPLIER = 2.0;           // Exponential backoff: 2x, 4x, 8x, etc.

    private readonly BikeapelagoDbContext _context;
    private readonly ILogger<RegionalElevationService> _logger;
    private readonly IElevationJobQueue _jobQueue;

    public RegionalElevationService(
        BikeapelagoDbContext context,
        ILogger<RegionalElevationService> logger,
        IElevationJobQueue jobQueue)
    {
        _context = context;
        _logger = logger;
        _jobQueue = jobQueue;
    }

    /// <summary>
    /// Get the SRTM tile code for a coordinate.
    /// Uses 1°×1° tiles (native SRTM resolution).
    /// Example: (40.5, -80.5) → "N40W081" (covers 40-41°N, 81-80°W)
    /// </summary>
    public static string GetRegionCode(double latitude, double longitude)
    {
        // Round down to nearest 1 degree tile
        int lat = (int)Math.Floor(latitude);
        int lon = (int)Math.Floor(longitude);

        string latStr = lat >= 0 ? $"N{Math.Abs(lat):D2}" : $"S{Math.Abs(lat):D2}";
        string lonStr = lon <= 0 ? $"W{Math.Abs(lon):D3}" : $"E{Math.Abs(lon):D3}";

        return $"{latStr}{lonStr}";
    }

    /// <summary>
    /// Get all tiles needed for a session's nodes.
    /// Calculates bounding box and returns all 1°×1° tiles that intersect.
    /// </summary>
    public async Task<List<string>> GetRequiredTilesAsync(Guid sessionId)
    {
        var nodes = await _context.MapNodes
            .Where(m => m.SessionId == sessionId && m.Location != null)
            .Select(m => new { Lat = m.Location!.Y, Lon = m.Location.X })
            .ToListAsync();

        if (nodes.Count == 0)
            return new();

        // Get bounding box
        var minLat = nodes.Min(n => n.Lat);
        var maxLat = nodes.Max(n => n.Lat);
        var minLon = nodes.Min(n => n.Lon);
        var maxLon = nodes.Max(n => n.Lon);

        // Add small buffer (5% of range or 0.05 degrees minimum)
        var latBuffer = Math.Max(0.05, (maxLat - minLat) * 0.05);
        var lonBuffer = Math.Max(0.05, (maxLon - minLon) * 0.05);

        minLat -= latBuffer;
        maxLat += latBuffer;
        minLon -= lonBuffer;
        maxLon += lonBuffer;

        // Collect all 1°×1° tiles that intersect the bounding box
        var tiles = new HashSet<string>();

        for (int lat = (int)Math.Floor(minLat); lat <= (int)Math.Floor(maxLat); lat++)
        {
            for (int lon = (int)Math.Floor(minLon); lon <= (int)Math.Floor(maxLon); lon++)
            {
                string latStr = lat >= 0 ? $"N{Math.Abs(lat):D2}" : $"S{Math.Abs(lat):D2}";
                string lonStr = lon <= 0 ? $"W{Math.Abs(lon):D3}" : $"E{Math.Abs(lon):D3}";
                tiles.Add($"{latStr}{lonStr}");
            }
        }

        return tiles.OrderBy(t => t).ToList();
    }

    /// <summary>
    /// Queue elevation downloads for a session.
    /// Intelligently chunks large downloads and uses exponential backoff for rate limiting.
    /// </summary>
    public async Task EnsureSessionTilesAsync(Guid sessionId)
    {
        try
        {
            var requiredTiles = await GetRequiredTilesAsync(sessionId);

            if (requiredTiles.Count == 0)
            {
                _logger.LogInformation($"Session {sessionId}: No tiles needed");
                return;
            }

            _logger.LogInformation($"Session {sessionId}: {requiredTiles.Count} tiles needed");

            // Decide chunking strategy based on tile count
            if (requiredTiles.Count <= TILES_BEFORE_CHUNKING)
            {
                // Small session: queue all tiles immediately
                _logger.LogInformation($"Session {sessionId}: Downloading all {requiredTiles.Count} tiles immediately (no chunking)");
                foreach (var tile in requiredTiles)
                {
                    await _jobQueue.QueueElevationDownloadAsync(tile, sessionId);
                }
            }
            else
            {
                // Large session: chunk with exponential backoff
                _logger.LogInformation($"Session {sessionId}: Chunking into batches of {TILES_PER_CHUNK} tiles");

                var chunks = requiredTiles
                    .Select((tile, index) => new { tile, index })
                    .GroupBy(x => x.index / TILES_PER_CHUNK)
                    .Select(g => g.Select(x => x.tile).ToList())
                    .ToList();

                for (int chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
                {
                    var chunk = chunks[chunkIndex];

                    // Calculate delay for this chunk (exponential backoff)
                    int delayMs = INITIAL_RETRY_DELAY_MS;
                    for (int i = 0; i < chunkIndex; i++)
                    {
                        delayMs = Math.Min((int)(delayMs * BACKOFF_MULTIPLIER), MAX_RETRY_DELAY_MS);
                    }

                    _logger.LogInformation($"Session {sessionId}: Queueing chunk {chunkIndex + 1}/{chunks.Count} ({chunk.Count} tiles, delay {delayMs}ms)");

                    foreach (var tile in chunk)
                    {
                        await _jobQueue.QueueElevationDownloadAsync(tile, sessionId, delayMs, chunkIndex);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error queuing elevation tiles for session {sessionId}: {ex.Message}");
        }
    }

    /// <summary>
    /// For backward compatibility - calls new method
    /// </summary>
    [Obsolete("Use EnsureSessionTilesAsync instead")]
    public async Task EnsureSessionRegionsAsync(Guid sessionId)
    {
        await EnsureSessionTilesAsync(sessionId);
    }

    /// <summary>
    /// Get elevation from PostGIS (which gets populated by background jobs).
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
            _logger.LogError($"Error querying elevation: {ex.Message}");
            return null;
        }
    }
}
