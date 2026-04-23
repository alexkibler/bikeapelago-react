using System;
using System.Collections.Generic;
using System.Linq;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Validators;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bikeapelago.Api.Tests.Unit;

public class SessionValidatorTests
{
    private readonly SessionValidator _validator;

    public SessionValidatorTests()
    {
        _validator = new SessionValidator(Mock.Of<ILogger<SessionValidator>>());
    }

    [Fact]
    public void ValidateNodeCheck_WithNoNodes_ReturnsError()
    {
        // Act
        var result = _validator.ValidateNodeCheck(new List<MapNode>(), Guid.NewGuid());

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("No valid nodes found to check.", result.Error);
        Assert.Empty(result.ValidNodes);
    }

    [Fact]
    public void ValidateNodeCheck_WithOnlyUnavailableNodes_ReturnsError()
    {
        // Arrange
        var nodes = new List<MapNode>
        {
            new() { Id = Guid.NewGuid(), State = "Hidden" },
            new() { Id = Guid.NewGuid(), State = "Checked" }
        };

        // Act
        var result = _validator.ValidateNodeCheck(nodes, Guid.NewGuid());

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("None of the submitted nodes are in an Available state.", result.Error);
        Assert.Empty(result.ValidNodes);
    }

    [Fact]
    public void ValidateNodeCheck_WithMixedNodes_FiltersAvailableNodes()
    {
        // Arrange
        var id2 = Guid.NewGuid();
        var id4 = Guid.NewGuid();
        var nodes = new List<MapNode>
        {
            new() { Id = Guid.NewGuid(), State = "Hidden" },
            new() { Id = id2, State = "Available" },
            new() { Id = Guid.NewGuid(), State = "Checked" },
            new() { Id = id4, State = "Available" }
        };

        // Act
        var result = _validator.ValidateNodeCheck(nodes, Guid.NewGuid());

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
        Assert.Equal(2, result.ValidNodes.Count);
        Assert.Contains(result.ValidNodes, n => n.Id == id2);
        Assert.Contains(result.ValidNodes, n => n.Id == id4);
    }

    [Fact]
    public void ValidateNodeCheck_WithAllAvailableNodes_ReturnsAllNodes()
    {
        // Arrange
        var nodes = new List<MapNode>
        {
            new() { Id = Guid.NewGuid(), State = "Available" },
            new() { Id = Guid.NewGuid(), State = "Available" }
        };

        // Act
        var result = _validator.ValidateNodeCheck(nodes, Guid.NewGuid());

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
        Assert.Equal(2, result.ValidNodes.Count);
    }
}
