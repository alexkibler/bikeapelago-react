using System;
using System.Collections.Generic;
using System.Linq;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Services;
using Xunit;

namespace Bikeapelago.Api.Tests.Unit;

public class FitAnalysisServiceTests
{
    [Fact]
    public void FindReachedNodes_CorrectlyIdentifiesNearNodes()
    {
        // Center of Pittsburgh
        double lat = 40.4406;
        double lon = -79.9959;

        var path = new List<PathPoint>
        {
            new PathPoint { Lat = lat, Lon = lon }
        };

        var nodes = new List<MapNode>
        {
            // 1. Very close (reached) ~15m away
            new MapNode { Id = Guid.NewGuid(), Name = "Close", Lat = lat + 0.0001, Lon = lon + 0.0001 },
            // 2. Far away (unreached)
            new MapNode { Id = Guid.NewGuid(), Name = "Far", Lat = lat + 1.0, Lon = lon + 1.0 },
            // 3. Just outside 30m range (~44m)
            new MapNode { Id = Guid.NewGuid(), Name = "Boundary", Lat = lat + 0.0004, Lon = lon }
        };

        var reached = FitAnalysisService.FindReachedNodes(path, nodes);

        Assert.Single(reached);
        Assert.Equal("Close", nodes.First(n => n.Id == reached[0].Id).Name);
    }

    [Fact]
    public void FindReachedNodes_HandlesLargeDatasets()
    {
        // 7,200 points (1 per second for 2 hours)
        var path = Enumerable.Range(0, 7200)
            .Select(i => new PathPoint { Lat = 40.0 + (i * 0.00001), Lon = -80.0 + (i * 0.00001) })
            .ToList();

        // 50 nodes
        var nodes = Enumerable.Range(0, 50)
            .Select(i => new MapNode 
            { 
                Id = Guid.NewGuid(), 
                Lat = 40.0 + (i * 0.001), 
                Lon = -80.0 + (i * 0.001) 
            })
            .ToList();

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var reached = FitAnalysisService.FindReachedNodes(path, nodes);
        watch.Stop();

        // Even without optimization, it should finish, but we note the time.
        // On a modern CPU, 360,000 Haversines take ~20-50ms. 
        // The "inefficiency" is relative to the scale, but it adds up with many users.
        Assert.NotEmpty(reached);
    }
}
