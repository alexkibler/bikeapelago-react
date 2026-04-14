using System;
using System.Collections.Generic;
using System.Linq;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Services;
using NetTopologySuite.Geometries;
using Xunit;

namespace Bikeapelago.Api.Tests.Unit;

public class GeographicSortingServiceTests
{
    private static MapNode NodeAt(double lat, double lon, string name = "node") => new MapNode
    {
        Id = Guid.NewGuid(),
        Name = name,
        Location = new Point(lon, lat) { SRID = 4326 }
    };

    private static Point PointAt(double lat, double lon) =>
        new Point(lon, lat) { SRID = 4326 };

    [Fact]
    public void SortByNearestNeighbor_EmptyList_ReturnsEmpty()
    {
        var result = GeographicSortingService.SortByNearestNeighbor(
            PointAt(40.0, -80.0), new List<MapNode>());

        Assert.Empty(result);
    }

    [Fact]
    public void SortByNearestNeighbor_NullList_ReturnsEmpty()
    {
        var result = GeographicSortingService.SortByNearestNeighbor(
            PointAt(40.0, -80.0), null!);

        Assert.Empty(result);
    }

    [Fact]
    public void SortByNearestNeighbor_NullStartingLocation_ThrowsArgumentException()
    {
        var nodes = new List<MapNode> { NodeAt(40.0, -80.0) };

        Assert.Throws<ArgumentException>(() =>
            GeographicSortingService.SortByNearestNeighbor(null!, nodes));
    }

    [Fact]
    public void SortByNearestNeighbor_SingleNode_ReturnsThatNode()
    {
        var node = NodeAt(40.4406, -79.9959, "only");

        var result = GeographicSortingService.SortByNearestNeighbor(
            PointAt(40.0, -80.0), new List<MapNode> { node });

        Assert.Single(result);
        Assert.Equal("only", result[0].Name);
    }

    [Fact]
    public void SortByNearestNeighbor_PicksNearestNodeFirst()
    {
        // Start at (0,0); "near" is 110m away, "far" is ~111km away
        var near = NodeAt(0.001, 0.0, "near");
        var far  = NodeAt(1.0,   0.0, "far");

        var result = GeographicSortingService.SortByNearestNeighbor(
            PointAt(0.0, 0.0), new List<MapNode> { far, near });

        Assert.Equal("near", result[0].Name);
        Assert.Equal("far",  result[1].Name);
    }

    [Fact]
    public void SortByNearestNeighbor_ChainsProperly()
    {
        // Nodes at increasing latitudes; nearest-neighbor from origin should visit A→B→C
        var a = NodeAt(0.01, 0.0, "A");
        var b = NodeAt(1.0,  0.0, "B");
        var c = NodeAt(2.0,  0.0, "C");

        var result = GeographicSortingService.SortByNearestNeighbor(
            PointAt(0.0, 0.0), new List<MapNode> { c, b, a });

        Assert.Equal(new[] { "A", "B", "C" }, result.Select(n => n.Name));
    }

    [Fact]
    public void SortByNearestNeighbor_ReturnsAllNodes()
    {
        var nodes = Enumerable.Range(0, 20)
            .Select(i => NodeAt(i * 0.1, 0.0, $"node{i}"))
            .ToList();

        var result = GeographicSortingService.SortByNearestNeighbor(
            PointAt(0.0, 0.0), nodes);

        Assert.Equal(20, result.Count);
        // Every input node appears in the output exactly once
        Assert.Equal(
            nodes.Select(n => n.Id).OrderBy(id => id),
            result.Select(n => n.Id).OrderBy(id => id));
    }

    [Fact]
    public void SortByNearestNeighbor_InputOrderDoesNotAffectOutput()
    {
        // Same three nodes provided in two different orders should produce the same sorted sequence
        var a = NodeAt(0.01, 0.0, "A");
        var b = NodeAt(1.0,  0.0, "B");
        var c = NodeAt(2.0,  0.0, "C");

        var start = PointAt(0.0, 0.0);
        var result1 = GeographicSortingService.SortByNearestNeighbor(start, new List<MapNode> { a, b, c });
        var result2 = GeographicSortingService.SortByNearestNeighbor(start, new List<MapNode> { c, b, a });

        Assert.Equal(result1.Select(n => n.Name), result2.Select(n => n.Name));
    }
}
