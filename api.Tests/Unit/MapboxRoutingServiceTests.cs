using System;
using System.Collections.Generic;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite.Geometries;
using Xunit;

namespace Bikeapelago.Api.Tests.Unit;

/// <summary>
/// Tests for the pure (non-HTTP) methods on MapboxRoutingService:
/// GenerateGpx and ChunkNodes-adjacent logic.
/// </summary>
public class MapboxRoutingServiceTests
{
    private static MapboxRoutingService CreateService()
    {
        // GenerateGpx never touches HttpClient, IMemoryCache, or the Mapbox API key,
        // so we can safely pass lightweight stubs for those.
        return new MapboxRoutingService(
            httpClient:    null!,
            logger:        Mock.Of<ILogger<MapboxRoutingService>>(),
            configuration: Mock.Of<IConfiguration>(),   // indexer returns null → key falls back to ""
            memoryCache:   Mock.Of<IMemoryCache>(),
            geographicSortingService: Mock.Of<IGeographicSortingService>());
    }

    private static MapNode NodeAt(Guid id, string name, double lat, double lon) => new MapNode
    {
        Id = id,
        Name = name,
        Location = new Point(lon, lat) { SRID = 4326 }
    };

    // ── Turn-by-turn (track) mode ───────────────────────────────────────────────

    [Fact]
    public void GenerateGpx_TurnByTurn_ProducesTrackStructure()
    {
        var geometry = new List<List<double>>
        {
            new() { -79.9, 40.4 },
            new() { -79.8, 40.5 }
        };

        var gpx = CreateService().GenerateGpx(geometry, new List<MapNode>(), turnByTurn: true);

        Assert.Contains("<trk>",    gpx);
        Assert.Contains("<trkseg>", gpx);
        Assert.Contains("<trkpt",   gpx);
        Assert.DoesNotContain("<rte>",   gpx);
        Assert.DoesNotContain("<rtept>", gpx);
    }

    [Fact]
    public void GenerateGpx_TurnByTurn_WritesCorrectLatLon()
    {
        // Geometry is stored as [lon, lat]; GPX attributes must be lat="…" lon="…"
        var geometry = new List<List<double>> { new() { -79.9959, 40.4406 } };

        var gpx = CreateService().GenerateGpx(geometry, new List<MapNode>(), turnByTurn: true);

        Assert.Contains("lat=\"40.4406\"",  gpx);
        Assert.Contains("lon=\"-79.9959\"", gpx);
    }

    // ── Route (waypoints) mode ─────────────────────────────────────────────────

    [Fact]
    public void GenerateGpx_RouteMode_ProducesRouteStructure()
    {
        var node = NodeAt(Guid.NewGuid(), "Test Point", 40.4406, -79.9959);

        var gpx = CreateService().GenerateGpx(
            new List<List<double>>(), new List<MapNode> { node }, turnByTurn: false);

        Assert.Contains("<rte>",    gpx);
        Assert.Contains("<rtept",   gpx);
        Assert.Contains("Test Point", gpx);
        Assert.DoesNotContain("<trk>", gpx);
    }

    [Fact]
    public void GenerateGpx_RouteMode_UsesOriginalLocationWhenNoSnap()
    {
        var node = NodeAt(Guid.NewGuid(), "Node", 40.44, -79.99);

        var gpx = CreateService().GenerateGpx(
            new List<List<double>>(), new List<MapNode> { node }, turnByTurn: false);

        Assert.Contains("lat=\"40.44\"",  gpx);
        Assert.Contains("lon=\"-79.99\"", gpx);
    }

    [Fact]
    public void GenerateGpx_RouteMode_PrefersSnappedLocationOverOriginal()
    {
        var nodeId = Guid.NewGuid();
        var node = NodeAt(nodeId, "Node", 40.44, -79.99); // original (off-road)

        var snapped = new Dictionary<Guid, List<double>>
        {
            [nodeId] = new() { -80.00, 40.55 } // [lon, lat] from routing engine
        };

        var gpx = CreateService().GenerateGpx(
            new List<List<double>>(), new List<MapNode> { node },
            turnByTurn: false, snappedLocations: snapped);

        // Snapped position must appear
        Assert.Contains("lat=\"40.55\"", gpx);
        Assert.Contains("lon=\"-80\"",   gpx);
        // Original must NOT appear
        Assert.DoesNotContain("lat=\"40.44\"",  gpx);
        Assert.DoesNotContain("lon=\"-79.99\"", gpx);
    }

    [Fact]
    public void GenerateGpx_RouteMode_FallsBackToOriginalWhenNodeNotInSnappedDict()
    {
        var nodeId = Guid.NewGuid();
        var node = NodeAt(nodeId, "Node", 40.44, -79.99);

        // snappedLocations dict is present but contains a different node
        var snapped = new Dictionary<Guid, List<double>>
        {
            [Guid.NewGuid()] = new() { -80.00, 40.55 }
        };

        var gpx = CreateService().GenerateGpx(
            new List<List<double>>(), new List<MapNode> { node },
            turnByTurn: false, snappedLocations: snapped);

        Assert.Contains("lat=\"40.44\"",  gpx);
        Assert.Contains("lon=\"-79.99\"", gpx);
    }

    [Fact]
    public void GenerateGpx_RouteMode_EscapesSpecialCharactersInNodeName()
    {
        var node = NodeAt(Guid.NewGuid(), "Node <A> & \"B\"", 40.44, -79.99);

        var gpx = CreateService().GenerateGpx(
            new List<List<double>>(), new List<MapNode> { node }, turnByTurn: false);

        Assert.Contains("&lt;A&gt;", gpx);
        Assert.Contains("&amp;",     gpx);
        // Raw angle brackets and ampersand must not appear inside the name element
        Assert.DoesNotContain("<A>", gpx);
    }

    [Fact]
    public void GenerateGpx_RouteMode_SkipsNodeWithNullLocation()
    {
        var nodeWithLocation    = NodeAt(Guid.NewGuid(), "Valid", 40.44, -79.99);
        var nodeWithoutLocation = new MapNode { Id = Guid.NewGuid(), Name = "No Location" };

        var gpx = CreateService().GenerateGpx(
            new List<List<double>>(),
            new List<MapNode> { nodeWithoutLocation, nodeWithLocation },
            turnByTurn: false);

        Assert.Contains("Valid",       gpx);
        Assert.DoesNotContain("No Location", gpx);
    }

    [Fact]
    public void GenerateGpx_RouteMode_EmptyNodeList_ProducesValidGpxFrame()
    {
        var gpx = CreateService().GenerateGpx(
            new List<List<double>>(), new List<MapNode>(), turnByTurn: false);

        Assert.StartsWith("<?xml", gpx.TrimStart());
        Assert.Contains("<gpx",  gpx);
        Assert.Contains("</gpx>", gpx);
        Assert.Contains("<rte>",  gpx);
        Assert.Contains("</rte>", gpx);
    }
}
