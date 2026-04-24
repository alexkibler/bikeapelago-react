using System;
using System.Collections.Generic;
using System.Linq;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bikeapelago.Api.Tests.Unit;

public class SinglePlayerSeedGeneratorTests
{
    private readonly SinglePlayerSeedGenerator _generator;

    public SinglePlayerSeedGeneratorTests()
    {
        _generator = new SinglePlayerSeedGenerator(NullLogger<SinglePlayerSeedGenerator>.Instance);
    }

    [Fact]
    public void GenerateSeed_ShouldAssignItemsToAllNodes()
    {
        // Arrange
        var session = new GameSession 
        { 
            CenterLat = 40.4, 
            CenterLon = -79.9, 
            Radius = 5000, 
            ProgressionMode = "quadrant" 
        };

        var nodes = new List<MapNode>();
        for (int i = 0; i < 20; i++)
        {
            nodes.Add(new MapNode
            {
                Id = Guid.NewGuid(),
                Lat = 40.4 + (i * 0.001), 
                Lon = -79.9 + (i * 0.001)
            });
        }

        // Act
        _generator.GenerateSeed(session, nodes);

        // Assert
        foreach (var node in nodes)
        {
            Assert.True(node.ArrivalRewardItemId.HasValue, "Arrival reward was not assigned.");
            Assert.True(node.PrecisionRewardItemId.HasValue, "Precision reward was not assigned.");
            Assert.NotNull(node.ArrivalRewardItemName);
            Assert.NotNull(node.PrecisionRewardItemName);
        }
        
        var allAssignedItems = nodes.Select(n => n.ArrivalRewardItemId!.Value)
            .Concat(nodes.Select(n => n.PrecisionRewardItemId!.Value))
            .ToList();
            
        Assert.Contains(ItemDefinitions.Macguffin, allAssignedItems);
        Assert.Contains(ItemDefinitions.NorthPass, allAssignedItems);
        Assert.Contains(ItemDefinitions.SouthPass, allAssignedItems);
        Assert.Contains(ItemDefinitions.EastPass, allAssignedItems);
        Assert.Contains(ItemDefinitions.WestPass, allAssignedItems);
    }
    
    [Fact]
    public void GenerateSeed_MissingGeographicMetrics_ThrowsException()
    {
        // Arrange
        var session = new GameSession { ProgressionMode = "quadrant" };
        var nodes = new List<MapNode> { new MapNode { Lat = 40.4, Lon = -79.9 } };

        // Act & Assert
        Assert.Throws<Exception>(() => _generator.GenerateSeed(session, nodes));
    }

    // ── Macguffin placement tests ────────────────────────────────────────────

    [Fact]
    public void GenerateSeed_NoNodeReceivesTwoMacguffins()
    {
        var session = MakeQuadrantSession();
        var nodes = MakeLinearNodes(20);

        _generator.GenerateSeed(session, nodes);

        foreach (var node in nodes)
        {
            bool twoMacguffins =
                node.ArrivalRewardItemId   == ItemDefinitions.Macguffin &&
                node.PrecisionRewardItemId == ItemDefinitions.Macguffin;

            Assert.False(twoMacguffins,
                $"Node {node.Id} has Macguffins in both arrival and precision slots.");
        }
    }

    [Fact]
    public void GenerateSeed_EachMacguffinIsOnADistinctNode()
    {
        var session = MakeQuadrantSession();
        var nodes = MakeLinearNodes(20);

        _generator.GenerateSeed(session, nodes);

        int totalMacguffinItems = nodes.Sum(n =>
            (n.ArrivalRewardItemId   == ItemDefinitions.Macguffin ? 1 : 0) +
            (n.PrecisionRewardItemId == ItemDefinitions.Macguffin ? 1 : 0));

        int macguffinNodeCount = nodes.Count(n =>
            n.ArrivalRewardItemId   == ItemDefinitions.Macguffin ||
            n.PrecisionRewardItemId == ItemDefinitions.Macguffin);

        Assert.Equal(totalMacguffinItems, macguffinNodeCount);
    }

    [Fact]
    public void GenerateSeed_MacguffinsAvoidHubNodes_WhenOuterNodesCanAbsorbThem()
    {
        // 4 hub nodes (within 25% radius) + 8 outer nodes spread across all four quadrants.
        // With 12 nodes the item pool produces 4 Macguffins; 8 outer nodes each have 2
        // empty slots, so all Macguffins fit in outer (deeper-sphere) nodes with room to spare.
        var session = MakeQuadrantSession();
        var nodes = MakeGeographicNodes();

        _generator.GenerateSeed(session, nodes);

        const double CenterLat = 40.4;
        const double CenterLon = -79.9;
        const double HubRadius = 5000 * 0.25; // 1250 m

        var hubNodes = nodes
            .Where(n => HaversineMeters(CenterLat, CenterLon, n.Lat!.Value, n.Lon!.Value) <= HubRadius)
            .ToList();

        Assert.NotEmpty(hubNodes); // sanity-check the test data

        foreach (var hub in hubNodes)
        {
            Assert.NotEqual(ItemDefinitions.Macguffin, hub.ArrivalRewardItemId ?? 0);
            Assert.NotEqual(ItemDefinitions.Macguffin, hub.PrecisionRewardItemId ?? 0);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static GameSession MakeQuadrantSession() => new()
    {
        CenterLat = 40.4,
        CenterLon = -79.9,
        Radius    = 5000,
        ProgressionMode = "quadrant",
    };

    /// <summary>
    /// 20 nodes marching NE from the centre — used by tests that don't need
    /// specific quadrant geography.
    /// </summary>
    private static List<MapNode> MakeLinearNodes(int count)
    {
        var nodes = new List<MapNode>();
        for (int i = 0; i < count; i++)
            nodes.Add(new MapNode { Id = Guid.NewGuid(), Lat = 40.4 + i * 0.001, Lon = -79.9 + i * 0.001 });
        return nodes;
    }

    /// <summary>
    /// 12 nodes with known hub / outer positions:
    ///   4 hub nodes   within  ~1250 m of centre
    ///   8 outer nodes between ~2200 m – 3700 m, two per quadrant
    /// </summary>
    private static List<MapNode> MakeGeographicNodes() =>
    [
        // Hub (within 1250 m)
        new MapNode { Id = Guid.NewGuid(), Lat = 40.4000, Lon = -79.9000 },
        new MapNode { Id = Guid.NewGuid(), Lat = 40.4080, Lon = -79.9000 },
        new MapNode { Id = Guid.NewGuid(), Lat = 40.4000, Lon = -79.8920 },
        new MapNode { Id = Guid.NewGuid(), Lat = 40.3930, Lon = -79.9000 },
        // North quadrant
        new MapNode { Id = Guid.NewGuid(), Lat = 40.4200, Lon = -79.9000 },
        new MapNode { Id = Guid.NewGuid(), Lat = 40.4300, Lon = -79.9000 },
        // East quadrant
        new MapNode { Id = Guid.NewGuid(), Lat = 40.4000, Lon = -79.8720 },
        new MapNode { Id = Guid.NewGuid(), Lat = 40.4000, Lon = -79.8600 },
        // South quadrant
        new MapNode { Id = Guid.NewGuid(), Lat = 40.3800, Lon = -79.9000 },
        new MapNode { Id = Guid.NewGuid(), Lat = 40.3700, Lon = -79.9000 },
        // West quadrant
        new MapNode { Id = Guid.NewGuid(), Lat = 40.4000, Lon = -79.9280 },
        new MapNode { Id = Guid.NewGuid(), Lat = 40.4000, Lon = -79.9400 },
    ];

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6_371_000;
        double phi1 = lat1 * Math.PI / 180, phi2 = lat2 * Math.PI / 180;
        double dp   = (lat2 - lat1) * Math.PI / 180;
        double dl   = (lon2 - lon1) * Math.PI / 180;
        double a    = Math.Sin(dp / 2) * Math.Sin(dp / 2)
                    + Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(dl / 2) * Math.Sin(dl / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
