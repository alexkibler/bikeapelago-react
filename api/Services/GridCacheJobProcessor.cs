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
public class GridCacheJobProcessor : BackgroundService
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

            // Use the GridCacheService to build the cache
            using (var scope = _serviceProvider.CreateScope())
            {
                var gridCache = scope.ServiceProvider.GetRequiredService<GridCacheService>();
                await gridCache.BuildCacheForCellAsync(gridX, gridY, mode);
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
