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
            
        Assert.Contains(ItemDefinitions.Goal, allAssignedItems);
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
}
