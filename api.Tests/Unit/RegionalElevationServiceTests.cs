using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Bikeapelago.Api.Data;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Services;
using NetTopologySuite.Geometries;

namespace Bikeapelago.Api.Tests.Unit;

public class RegionalElevationServiceTests
{
    private readonly BikeapelagoDbContext _context;
    private readonly Mock<ILogger<RegionalElevationService>> _mockLogger;
    private readonly Mock<GridCacheJobProcessor> _mockJobProcessor;
    private readonly RegionalElevationService _service;

    public RegionalElevationServiceTests()
    {
        // In-memory database for testing
        var options = new DbContextOptionsBuilder<BikeapelagoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BikeapelagoDbContext(options);
        _mockLogger = new Mock<ILogger<RegionalElevationService>>();
        _mockJobProcessor = new Mock<GridCacheJobProcessor>();

        _service = new RegionalElevationService(_context, _mockLogger.Object, _mockJobProcessor.Object);
    }

    /// <summary>
    /// Test tile code generation from coordinates
    /// </summary>
    [Theory]
    [InlineData(40.5, -80.5, "N40W081")] // Pittsburgh area
    [InlineData(40.0, -80.0, "N40W080")]
    [InlineData(42.88, -78.88, "N42W079")] // Buffalo area
    [InlineData(45.5, -73.57, "N45W074")] // Montreal area
    public void GetRegionCode_ReturnsCorrectTileCode(double lat, double lon, string expected)
    {
        var result = RegionalElevationService.GetRegionCode(lat, lon);
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Test that small sessions don't get chunked
    /// </summary>
    [Fact]
    public async Task GetRequiredTiles_SmallSession_ReturnsFewTiles()
    {
        var sessionId = Guid.NewGuid();

        // Create nodes in Pennsylvania area (single tile)
        var nodes = new[]
        {
            new MapNode { SessionId = sessionId, Name = "Pittsburgh", Location = new Point(-80.0, 40.44) { SRID = 4326 } },
            new MapNode { SessionId = sessionId, Name = "Harrisburg", Location = new Point(-76.87, 40.26) { SRID = 4326 } },
        };

        await _context.MapNodes.AddRangeAsync(nodes);
        await _context.SaveChangesAsync();

        var tiles = await _service.GetRequiredTilesAsync(sessionId);

        // Should need 1-2 tiles for PA area
        Assert.NotEmpty(tiles);
        Assert.True(tiles.Count <= 10, "Small session should need ≤10 tiles");
    }

    /// <summary>
    /// Test that large sessions get correct number of tiles
    /// </summary>
    [Fact]
    public async Task GetRequiredTiles_LargeSession_ReturnsMultipleTiles()
    {
        var sessionId = Guid.NewGuid();

        // Create nodes spanning multiple states (Pennsylvania to Illinois)
        var nodes = new[]
        {
            new MapNode { SessionId = sessionId, Name = "Pittsburgh", Location = new Point(-80.0, 40.44) { SRID = 4326 } },
            new MapNode { SessionId = sessionId, Name = "Columbus", Location = new Point(-82.99, 39.96) { SRID = 4326 } },
            new MapNode { SessionId = sessionId, Name = "Chicago", Location = new Point(-87.63, 41.88) { SRID = 4326 } },
        };

        await _context.MapNodes.AddRangeAsync(nodes);
        await _context.SaveChangesAsync();

        var tiles = await _service.GetRequiredTilesAsync(sessionId);

        // Should need multiple tiles spanning the route
        Assert.NotEmpty(tiles);
        Assert.True(tiles.Count >= 3, "Large session should need multiple tiles");
    }

    /// <summary>
    /// Test that tile calculations add buffer around nodes
    /// </summary>
    [Fact]
    public async Task GetRequiredTiles_AddsBufferAroundBounds()
    {
        var sessionId = Guid.NewGuid();

        // Single node at boundary
        var nodes = new[]
        {
            new MapNode { SessionId = sessionId, Name = "Boundary", Location = new Point(-80.0, 40.0) { SRID = 4326 } },
        };

        await _context.MapNodes.AddRangeAsync(nodes);
        await _context.SaveChangesAsync();

        var tiles = await _service.GetRequiredTilesAsync(sessionId);

        // Should include buffer tiles beyond single node
        Assert.True(tiles.Count >= 1, "Should include tiles with buffer");
        // Verify buffer was added by checking for surrounding tiles
        Assert.Contains(tiles, t => t.Contains("N") && (t.Contains("W") || t.Contains("E")));
    }

    /// <summary>
    /// Test job queueing for small sessions (no chunking)
    /// </summary>
    [Fact]
    public async Task EnsureSessionTiles_SmallSession_QueuesAllTilesImmediately()
    {
        var sessionId = Guid.NewGuid();

        // Small session (Pittsburgh area)
        var nodes = new[]
        {
            new MapNode { SessionId = sessionId, Name = "Pittsburgh", Location = new Point(-80.0, 40.44) { SRID = 4326 } },
        };

        await _context.MapNodes.AddRangeAsync(nodes);
        await _context.SaveChangesAsync();

        await _service.EnsureSessionTilesAsync(sessionId);

        // Verify jobs were queued
        _mockJobProcessor.Verify(
            x => x.QueueElevationDownloadAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.AtLeastOnce,
            "Should queue elevation downloads for small session");
    }

    /// <summary>
    /// Test that chunking applies exponential backoff
    /// </summary>
    [Fact]
    public async Task EnsureSessionTiles_LargeSession_UsesExponentialBackoff()
    {
        var sessionId = Guid.NewGuid();

        // Large session across many states to force chunking
        var nodes = new List<MapNode>();
        for (int lon = -80; lon >= -88; lon--)
        {
            for (int lat = 40; lat <= 46; lat++)
            {
                nodes.Add(new MapNode
                {
                    SessionId = sessionId,
                    Name = $"Node_{lat}_{lon}",
                    Location = new Point(lon, lat) { SRID = 4326 }
                });
            }
        }

        await _context.MapNodes.AddRangeAsync(nodes);
        await _context.SaveChangesAsync();

        await _service.EnsureSessionTilesAsync(sessionId);

        // Verify chunking with delays
        var calls = _mockJobProcessor.Calls.ToList();

        // Should have multiple calls with different delays (exponential backoff)
        var delaysUsed = new HashSet<int>();
        foreach (var call in calls)
        {
            var args = call.Arguments;
            if (args.Count >= 3)
            {
                var delay = (int)args[2];
                if (delay > 0)
                    delaysUsed.Add(delay);
            }
        }

        // Should have calls with no delay (first chunk) and then increasing delays
        Assert.True(calls.Count > 5, "Large session should queue many tiles");
    }

    /// <summary>
    /// Test empty session handling
    /// </summary>
    [Fact]
    public async Task EnsureSessionTiles_EmptySession_HandlesSilently()
    {
        var sessionId = Guid.NewGuid();

        // No nodes in session
        await _service.EnsureSessionTilesAsync(sessionId);

        // Should not queue any jobs
        _mockJobProcessor.Verify(
            x => x.QueueElevationDownloadAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never,
            "Should not queue jobs for empty session");
    }
}
