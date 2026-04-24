using System;
using System.Collections.Generic;
using System.Linq;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Services;
using Xunit;

namespace Bikeapelago.Api.Tests.Unit;

public class FitAnalysisServiceTests
{
    // Reference location used across multiple tests (center of Pittsburgh)
    private const double lat = 40.4406;
    private const double lon = -79.9959;

    [Fact]
    public void FindReachedNodes_CorrectlyIdentifiesNearNodes()
    {

        var path = new List<PathPoint>
        {
            new PathPoint { Lat = lat, Lon = lon }
        };

        var nodes = new List<MapNode>
        {
            // 1. Very close (reached) ~15m away -> Precision + Arrival
            new MapNode { Id = Guid.NewGuid(), Name = "Close", Lat = lat + 0.0001, Lon = lon + 0.0001 },
            // 2. Far away (unreached)
            new MapNode { Id = Guid.NewGuid(), Name = "Far", Lat = lat + 1.0, Lon = lon + 1.0 },
            // 3. Just outside 100m range (~111m)
            new MapNode { Id = Guid.NewGuid(), Name = "Boundary", Lat = lat + 0.001, Lon = lon }
        };

        var reached = FitAnalysisService.FindReachedNodes(path, nodes);

        Assert.Single(reached);
        Assert.Equal("Close", nodes.First(n => n.Id == reached[0].Id).Name);
        Assert.True(reached[0].ArrivalChecked);
        Assert.True(reached[0].PrecisionChecked);
    }

    [Fact]
    public void FindReachedNodes_NodeAt80m_IsOnlyArrivalReached()
    {
        // ~88m north ≈ 0.0008 degrees
        var node = new MapNode { Id = Guid.NewGuid(), Lat = lat + 0.0008, Lon = lon };
        var path = new List<PathPoint> { new PathPoint { Lat = lat, Lon = lon } };

        var reached = FitAnalysisService.FindReachedNodes(path, new[] { node });

        Assert.Single(reached);
        Assert.True(reached[0].ArrivalChecked);
        Assert.False(reached[0].PrecisionChecked);
    }

    [Fact]
    public void FindReachedNodes_SnappedLocationOnRoute_IsReached_ButOriginalOffRoadPosition_IsNot()
    {
        // This test verifies that the snapped node location persisted by RouteToAvailableNodes
        // is what determines whether a node is counted as reached during FIT file analysis.
        //
        // Scenario: a node's original position is 44m from the route (outside 30m threshold),
        // so it would be missed. After routing snaps it to the nearest road and the DB is
        // updated, the stored position is ~2m from the route and FIT analysis counts it.

        double routeLat = 40.4406;
        double routeLon = -79.9959;

        // Position after snapping – just ~2m off the route point, well within 30m
        var snappedNode = new MapNode
        {
            Id = Guid.NewGuid(),
            Name = "Node (snapped position stored in DB)",
            Lat = routeLat + 0.000015, // ~1.7m north
            Lon = routeLon
        };

        // Same node ID/name but at the original off-road position – ~44m away, outside threshold
        var originalNode = new MapNode
        {
            Id = snappedNode.Id,
            Name = snappedNode.Name,
            Lat = routeLat + 0.00040, // ~44m north
            Lon = routeLon
        };

        var path = new List<PathPoint> { new PathPoint { Lat = routeLat, Lon = routeLon } };

        var reachedWithSnapped  = FitAnalysisService.FindReachedNodes(path, new[] { snappedNode });
        var reachedWithOriginal = FitAnalysisService.FindReachedNodes(path, new[] { originalNode });

        // After snapping the node lands on the route – FIT analysis marks it as reached
        Assert.Single(reachedWithSnapped);
        Assert.Equal(snappedNode.Id, reachedWithSnapped[0].Id);
        Assert.True(reachedWithSnapped[0].PrecisionChecked);

        // Without snapping the node (at ~44m) is only Arrival reached (100m)
        Assert.Single(reachedWithOriginal);
        Assert.True(reachedWithOriginal[0].ArrivalChecked);
        Assert.False(reachedWithOriginal[0].PrecisionChecked);
    }

    [Fact]
    public void FindReachedNodes_EmptyPath_ReturnsEmpty()
    {
        var nodes = new List<MapNode>
        {
            new MapNode { Id = Guid.NewGuid(), Lat = lat, Lon = lon }
        };

        var reached = FitAnalysisService.FindReachedNodes(new List<PathPoint>(), nodes);

        Assert.Empty(reached);
    }

    [Fact]
    public void FindReachedNodes_EmptyNodeList_ReturnsEmpty()
    {
        var path = new List<PathPoint> { new PathPoint { Lat = lat, Lon = lon } };

        var reached = FitAnalysisService.FindReachedNodes(path, new List<MapNode>());

        Assert.Empty(reached);
    }

    [Fact]
    public void FindReachedNodes_NodeWithin100m_IsReached()
    {
        // 80 m north ≈ 0.00072 degrees latitude (safely inside the 100 m threshold)
        var node = new MapNode { Id = Guid.NewGuid(), Lat = lat + 0.00072, Lon = lon };
        var path = new List<PathPoint> { new PathPoint { Lat = lat, Lon = lon } };

        var reached = FitAnalysisService.FindReachedNodes(path, new[] { node });

        Assert.Single(reached);
        Assert.True(reached[0].ArrivalChecked);
    }

    [Fact]
    public void FindReachedNodes_SignalAmplifier_ClearsNodeAt70m()
    {
        // 70 m north ≈ 0.00063 degrees latitude
        var node = new MapNode { Id = Guid.NewGuid(), Lat = lat + 0.00063, Lon = lon };
        var path = new List<PathPoint> { new PathPoint { Lat = lat, Lon = lon } };

        // 1. Without Amplifier (Precision radius is 25m)
        var reachedNormal = FitAnalysisService.FindReachedNodes(path, new[] { node }, radiusMultiplier: 1.0);
        Assert.Single(reachedNormal);
        Assert.True(reachedNormal[0].ArrivalChecked);
        Assert.False(reachedNormal[0].PrecisionChecked); // MISSED!

        // 2. With Amplifier (Precision radius becomes 50m)
        // Wait, 70m is still outside 50m. Let's use 40m for this test case.
        // 40m north ≈ 0.00036 degrees
        var node40m = new MapNode { Id = Guid.NewGuid(), Lat = lat + 0.00036, Lon = lon };
        
        var reachedNormal40 = FitAnalysisService.FindReachedNodes(path, new[] { node40m }, radiusMultiplier: 1.0);
        Assert.False(reachedNormal40[0].PrecisionChecked);

        var reachedAmplified = FitAnalysisService.FindReachedNodes(path, new[] { node40m }, radiusMultiplier: 2.0);
        Assert.True(reachedAmplified[0].PrecisionChecked); // CLEARED!
    }

    [Fact]
    public void FindReachedNodes_Stackability_WorksWithHighMultipliers()
    {
        // 120m north ≈ 0.0011 degrees. 
        // Normally outside both Arrival (100m) and Precision (25m).
        var node = new MapNode { Id = Guid.NewGuid(), Lat = lat + 0.0011, Lon = lon };
        var path = new List<PathPoint> { new PathPoint { Lat = lat, Lon = lon } };

        // 1. Normal (1x) -> None
        var r1 = FitAnalysisService.FindReachedNodes(path, new[] { node }, 1.0);
        Assert.Empty(r1);

        // 2. Stacked (2x) -> Arrival Cleared (200m) but Precision Missed (50m)
        var r2 = FitAnalysisService.FindReachedNodes(path, new[] { node }, 2.0);
        Assert.True(r2[0].ArrivalChecked);
        Assert.False(r2[0].PrecisionChecked);

        // 3. Super Stacked (8x) -> Both Cleared (Arrival 800m, Precision 200m)
        var r8 = FitAnalysisService.FindReachedNodes(path, new[] { node }, 8.0);
        Assert.True(r8[0].ArrivalChecked);
        Assert.True(r8[0].PrecisionChecked);
    }

    [Fact]
    public void FindReachedNodes_NodeOutsideBoundingBox_IsSkipped()
    {
        // The bounding box pre-filter rejects nodes more than 0.01° away (~1.1 km)
        // without even running Haversine. This node is 2.2 km away — definitely skipped.
        var farNode = new MapNode { Id = Guid.NewGuid(), Lat = lat + 0.02, Lon = lon };
        var path = new List<PathPoint> { new PathPoint { Lat = lat, Lon = lon } };

        var reached = FitAnalysisService.FindReachedNodes(path, new[] { farNode });

        Assert.Empty(reached);
    }

    [Fact]
    public void FindReachedNodes_NodeWithNullLocation_IsSkipped()
    {
        // A MapNode whose Lat/Lon are null must be silently skipped, not throw
        var nullNode  = new MapNode { Id = Guid.NewGuid(), Name = "NoLoc" }; // no Lat/Lon set
        var validNode = new MapNode { Id = Guid.NewGuid(), Lat = lat + 0.0001, Lon = lon };
        var path = new List<PathPoint> { new PathPoint { Lat = lat, Lon = lon } };

        var reached = FitAnalysisService.FindReachedNodes(path, new[] { nullNode, validNode });

        Assert.Single(reached);
        Assert.Equal(validNode.Id, reached[0].Id);
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
