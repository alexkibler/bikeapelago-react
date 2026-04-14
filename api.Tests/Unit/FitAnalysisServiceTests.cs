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

        // Without snapping the node would have been missed entirely
        Assert.Empty(reachedWithOriginal);
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
    public void FindReachedNodes_NodeWithin30m_IsReached()
    {
        // 25 m north ≈ 0.000225 degrees latitude (safely inside the 30 m threshold)
        var node = new MapNode { Id = Guid.NewGuid(), Lat = lat + 0.000225, Lon = lon };
        var path = new List<PathPoint> { new PathPoint { Lat = lat, Lon = lon } };

        var reached = FitAnalysisService.FindReachedNodes(path, new[] { node });

        Assert.Single(reached);
    }

    [Fact]
    public void FindReachedNodes_NodeJustBeyond30m_IsNotReached()
    {
        // 35 m north ≈ 0.000315 degrees latitude — clearly outside 30 m threshold
        var node = new MapNode { Id = Guid.NewGuid(), Lat = lat + 0.000315, Lon = lon };
        var path = new List<PathPoint> { new PathPoint { Lat = lat, Lon = lon } };

        var reached = FitAnalysisService.FindReachedNodes(path, new[] { node });

        Assert.Empty(reached);
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
