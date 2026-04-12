using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using NetTopologySuite.Geometries;

namespace Bikeapelago.Api.Tests.Unit;

public class NodeGenerationServiceTests
{
    private readonly Mock<IOsmDiscoveryService> _osmDiscoveryServiceMock;
    private readonly Mock<IMapNodeRepository> _nodeRepositoryMock;
    private readonly Mock<IGameSessionRepository> _sessionRepositoryMock;
    private readonly Mock<ILogger<NodeGenerationService>> _loggerMock;
    private readonly NodeGenerationService _service;

    public NodeGenerationServiceTests()
    {
        _osmDiscoveryServiceMock = new Mock<IOsmDiscoveryService>();
        _nodeRepositoryMock = new Mock<IMapNodeRepository>();
        _sessionRepositoryMock = new Mock<IGameSessionRepository>();
        _loggerMock = new Mock<ILogger<NodeGenerationService>>();

        _service = new NodeGenerationService(
            _osmDiscoveryServiceMock.Object,
            _nodeRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateNodesAsync_ShouldFail_WhenSessionIsAlreadyActive()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = sessionId,
            Status = SessionStatus.Active
        };

        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        var request = new NodeGenerationRequest
        {
            SessionId = sessionId,
            CenterLat = 40.0,
            CenterLon = -74.0,
            Radius = 1000,
            NodeCount = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GenerateNodesAsync(request));
        Assert.Contains("status", exception.Message);
        Assert.Contains("SetupInProgress", exception.Message);
        
        // Verify delete was NOT called
        _nodeRepositoryMock.Verify(r => r.DeleteBySessionIdAsync(sessionId), Times.Never);
    }

    [Fact]
    public async Task GenerateNodesAsync_ShouldFail_WhenNodesAreAlreadyChecked()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = sessionId,
            Status = SessionStatus.SetupInProgress // Status might be okay, but nodes are checked
        };

        var existingNodes = new List<MapNode>
        {
            new MapNode { SessionId = sessionId, State = "Checked" },
            new MapNode { SessionId = sessionId, State = "Available" }
        };

        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        _nodeRepositoryMock.Setup(r => r.GetBySessionIdAsync(sessionId))
            .ReturnsAsync(existingNodes);

        var request = new NodeGenerationRequest
        {
            SessionId = sessionId,
            CenterLat = 40.0,
            CenterLon = -74.0,
            Radius = 1000,
            NodeCount = 10
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GenerateNodesAsync(request));
        Assert.Contains("already has checked nodes", exception.Message);

        // Verify delete was NOT called
        _nodeRepositoryMock.Verify(r => r.DeleteBySessionIdAsync(sessionId), Times.Never);
    }

    [Fact]
    public async Task GenerateNodesAsync_ShouldSucceed_WhenSessionIsSetupInProgressAndNoCheckedNodes()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new GameSession
        {
            Id = sessionId,
            Status = SessionStatus.SetupInProgress
        };

        var existingNodes = new List<MapNode>
        {
            new MapNode { SessionId = sessionId, State = "Available" },
            new MapNode { SessionId = sessionId, State = "Hidden" }
        };

        var discoveredNodes = new List<DiscoveryPoint>
        {
            new DiscoveryPoint(-74.01, 40.01),
            new DiscoveryPoint(-74.02, 40.02)
        };

        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        _nodeRepositoryMock.Setup(r => r.GetBySessionIdAsync(sessionId))
            .ReturnsAsync(existingNodes);

        _osmDiscoveryServiceMock.Setup(r => r.GetRandomNodesAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), 
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>()))
            .ReturnsAsync(discoveredNodes);

        var request = new NodeGenerationRequest
        {
            SessionId = sessionId,
            CenterLat = 40.0,
            CenterLon = -74.0,
            Radius = 1000,
            NodeCount = 2
        };

        // Act
        var result = await _service.GenerateNodesAsync(request);

        // Assert
        Assert.Equal(2, result);
        _nodeRepositoryMock.Verify(r => r.DeleteBySessionIdAsync(sessionId), Times.Once);
        _nodeRepositoryMock.Verify(r => r.CreateRangeAsync(It.Is<IEnumerable<MapNode>>(l => l.Count() == 2)), Times.Once);
        _sessionRepositoryMock.Verify(r => r.UpdateAsync(It.Is<GameSession>(s => s.Status == SessionStatus.Active)), Times.Once);
    }
}
