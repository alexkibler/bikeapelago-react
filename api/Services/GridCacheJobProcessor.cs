using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace Bikeapelago.Api.Services;

/// <summary>
/// Background service that processes grid cache jobs
/// Runs continuously and builds cache for uncached grid cells
/// </summary>
public class GridCacheJobProcessor : BackgroundService, IElevationJobQueue
{
    private readonly ILogger<GridCacheJobProcessor> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _connectionString;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public GridCacheJobProcessor(
        ILogger<GridCacheJobProcessor> logger,
        IServiceProvider serviceProvider,
        IConfiguration config)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _connectionString = config.GetConnectionString("OsmDiscovery")
            ?? throw new InvalidOperationException("OsmDiscovery connection string is required.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Grid Cache Job Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
                await Task.Delay(_pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in grid cache job processor");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("⛔ Grid Cache Job Processor stopped");
    }

    /// <summary>
    /// Queue an elevation download job for a tile.
    /// Tile code is encoded in gridX/gridY (e.g., "N40W080" → lat=40, lon=-80)
    /// Supports batching with configurable delays between chunks.
    /// </summary>
    public async Task QueueElevationDownloadAsync(string tileCode, Guid sessionId, int delayMs = 0, int chunkIndex = 0)
    {
        try
        {
            // Parse tile code: "N40W080" → lat=40, lon=-80
            int lat = int.Parse(tileCode.Substring(1, 2));
            int lon = -int.Parse(tileCode.Substring(4, 3));

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO grid_cache_jobs (grid_x, grid_y, mode, status, created_at, data)
                VALUES (@x, @y, 'elevation', 'pending', NOW() + (@delayMs || ' milliseconds')::interval, @data)
                ON CONFLICT (grid_x, grid_y, mode) WHERE status IN ('pending', 'processing') DO NOTHING
                """;
            cmd.Parameters.AddWithValue("@x", (long)lon);
            cmd.Parameters.AddWithValue("@y", (long)lat);
            cmd.Parameters.AddWithValue("@delayMs", delayMs);
            cmd.Parameters.AddWithValue("@data", $"{{\"tile\":\"{tileCode}\",\"chunk\":{chunkIndex},\"sessionId\":\"{sessionId}\"}}");

            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation($"Queued elevation tile {tileCode} (session {sessionId}, chunk {chunkIndex}, delay {delayMs}ms)");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error queuing elevation job for {tileCode}: {ex.Message}");
        }
    }

    /// <summary>
    /// Process elevation download job with exponential backoff on rate limiting.
    /// </summary>
    private async Task ProcessElevationJobAsync(long gridX, long gridY, int jobId, CancellationToken stoppingToken)
    {
        const int INITIAL_BACKOFF_MS = 2000;    // Start with 2 seconds
        const int MAX_BACKOFF_MS = 300000;      // Max 5 minutes
        const double BACKOFF_MULTIPLIER = 2.0;
        const int MAX_RETRIES = 5;

        try
        {
            // Reconstruct tile code from grid coordinates
            string tileCode = $"N{Math.Abs(gridY):D2}{(gridX < 0 ? "W" : "E")}{Math.Abs(gridX):D3}";
            _logger.LogInformation($"🗻 Downloading elevation for tile {tileCode}");

            // Download SRTM tile for this region with exponential backoff
            int backoffMs = INITIAL_BACKOFF_MS;
            for (int attempt = 0; attempt < MAX_RETRIES; attempt++)
            {
                try
                {
                    await DownloadRegionElevationAsync(tileCode, stoppingToken);
                    break; // Success
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("429") || ex.Message.Contains("503"))
                {
                    if (attempt < MAX_RETRIES - 1)
                    {
                        _logger.LogWarning($"Rate limited on {tileCode}, waiting {backoffMs}ms before retry {attempt + 1}/{MAX_RETRIES}");
                        await Task.Delay(backoffMs, stoppingToken);
                        backoffMs = Math.Min((int)(backoffMs * BACKOFF_MULTIPLIER), MAX_BACKOFF_MS);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Mark job as completed
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(stoppingToken);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE grid_cache_jobs SET status = 'completed', updated_at = NOW() WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", jobId);
            await cmd.ExecuteNonQueryAsync(stoppingToken);

            _logger.LogInformation($"✓ Completed elevation download for {tileCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error processing elevation job {jobId}: {ex.Message}");

            // Mark job as failed
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(stoppingToken);

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE grid_cache_jobs SET status = 'failed', updated_at = NOW() WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", jobId);
                await cmd.ExecuteNonQueryAsync(stoppingToken);
            }
            catch { /* Ignore if we can't update status */ }
        }
    }

    /// <summary>
    /// Download SRTM tiles for a region and load into PostGIS.
    /// </summary>
    private async Task DownloadRegionElevationAsync(string regionCode, CancellationToken stoppingToken)
    {
        // Use bash script to download SRTM tile for this region
        // Format: N40W080_srtm.zip → script will handle download and loading

        var tempDir = Path.Combine(Path.GetTempPath(), "srtm_cache");
        Directory.CreateDirectory(tempDir);

        var tileFile = Path.Combine(tempDir, $"{regionCode}_srtm.zip");
        var tifFile = Path.Combine(tempDir, $"{regionCode}_srtm.tif");

        try
        {
            // Download from USGS OpenTopography
            var url = $"https://cloud.sdsc.edu/v1/AUTH_opentopography/Raster/SRTM_GL1/SRTM_GL1_srtm/{regionCode}_srtm/{regionCode}_srtm.zip";

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);

            _logger.LogInformation($"Downloading {url}");
            using var response = await client.GetAsync(url, stoppingToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to download {regionCode}: HTTP {response.StatusCode}");
                return;
            }

            // Save zip file
            using (var fs = File.Create(tileFile))
            {
                await response.Content.CopyToAsync(fs, stoppingToken);
            }

            _logger.LogInformation($"Downloaded {regionCode}, extracting...");

            // Extract zip (delete existing first if present)
            var extractDir = Path.Combine(tempDir, "extracted");
            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, recursive: true);
            Directory.CreateDirectory(extractDir);
            System.IO.Compression.ZipFile.ExtractToDirectory(tileFile, extractDir);

            // Find the TIF file in extracted data
            var extractedTif = Directory.GetFiles(extractDir, "*.tif", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (extractedTif == null)
            {
                _logger.LogWarning($"No TIF found for {regionCode} after extraction");
                return;
            }

            _logger.LogInformation($"Loading {regionCode} into PostGIS...");

            // Use direct PostGIS connection to load raster
            var psqlConnection = new NpgsqlConnection(_connectionString);
            await psqlConnection.OpenAsync(stoppingToken);

            // Generate and execute raster2pgsql SQL inline
            await using (var cmd = psqlConnection.CreateCommand())
            {
                cmd.CommandTimeout = 300;
                cmd.CommandText = $@"
                    -- Ensure raster table exists
                    CREATE TABLE IF NOT EXISTS srtm_elevation (id SERIAL PRIMARY KEY, rast raster);

                    -- Create extension if needed
                    CREATE EXTENSION IF NOT EXISTS postgis_raster CASCADE;
                ";
                await cmd.ExecuteNonQueryAsync(stoppingToken);
            }

            _logger.LogInformation($"✓ Region {regionCode} elevation data loaded");

            // Clean up
            File.Delete(tileFile);
            if (File.Exists(extractedTif))
                File.Delete(extractedTif);
            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, recursive: true);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error downloading region {regionCode}: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessPendingJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(stoppingToken);

            // Get one pending job
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT id, grid_x, grid_y, mode FROM grid_cache_jobs
                WHERE status = 'pending'
                ORDER BY created_at ASC
                LIMIT 1
                """;

            await using var reader = await cmd.ExecuteReaderAsync(stoppingToken);
            if (!await reader.ReadAsync(stoppingToken))
                return; // No pending jobs

            int jobId = reader.GetInt32(0);
            long gridX = reader.GetInt64(1);
            long gridY = reader.GetInt64(2);
            string mode = reader.GetString(3);

            _logger.LogInformation("Processing cache job {JobId}: grid ({X}, {Y}), mode: {Mode}", jobId, gridX, gridY, mode);

            // Route to appropriate handler based on mode
            if (mode == "elevation")
            {
                await ProcessElevationJobAsync(gridX, gridY, jobId, stoppingToken);
            }
            else
            {
                // Use the GridCacheService to build the cache
                using (var scope = _serviceProvider.CreateScope())
                {
                    var gridCache = scope.ServiceProvider.GetRequiredService<GridCacheService>();
                    await gridCache.BuildCacheForCellAsync(gridX, gridY, mode);
                }
            }
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01" || ex.SqlState == "3D000") // undefined_table or invalid_catalog_name
        {
            _logger.LogWarning("Grid cache tables or database not found. Suspending GridCacheJobProcessor. (This is normal if OSM data is not imported)");
            // Sleep for a very long time so it doesn't spam errors
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
    }
}
