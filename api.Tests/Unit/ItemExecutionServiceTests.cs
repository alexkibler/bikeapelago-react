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

public class ItemExecutionServiceTests
{
    private readonly Mock<IMapNodeRepository> _nodeRepoMock;
    private readonly Mock<IGameSessionRepository> _sessionRepoMock;
    private readonly Mock<IOsmDiscoveryService> _osmDiscoveryMock;
    private readonly Mock<IArchipelagoService> _archipelagoMock;
    private readonly Mock<IProgressionEngineFactory> _engineFactoryMock;
    private readonly Mock<IProgressionEngine> _mockEngine;
    private readonly ItemExecutionService _service;

    public ItemExecutionServiceTests()
    {
        _nodeRepoMock = new Mock<IMapNodeRepository>();
        _sessionRepoMock = new Mock<IGameSessionRepository>();
        _osmDiscoveryMock = new Mock<IOsmDiscoveryService>();
        _archipelagoMock = new Mock<IArchipelagoService>();
        _engineFactoryMock = new Mock<IProgressionEngineFactory>();
        _mockEngine = new Mock<IProgressionEngine>();

        _engineFactoryMock.Setup(f => f.CreateEngine(It.IsAny<string>())).Returns(_mockEngine.Object);

        _service = new ItemExecutionService(
            _nodeRepoMock.Object,
            _sessionRepoMock.Object,
            _osmDiscoveryMock.Object,
            _archipelagoMock.Object,
            _engineFactoryMock.Object,
            Mock.Of<ILogger<ItemExecutionService>>());
    }

    [Fact]
    public async Task ExecuteDroneAsync_InArchipelagoMode_ShouldCompleteChecksAndNotifyArchipelago()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var session = new GameSession { Id = sessionId, Mode = "archipelago" };
        var node = new MapNode 
        { 
            Id = nodeId, 
            SessionId = sessionId,
            ApArrivalLocationId = 101,
            ApPrecisionLocationId = 102,
        };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);
        _nodeRepoMock.Setup(r => r.GetByIdAsync(nodeId)).ReturnsAsync(node);

        // Act
        var result = await _service.ExecuteDroneAsync(sessionId, nodeId);

        // Assert
        Assert.True(result);
        Assert.True(node.IsArrivalChecked);
        Assert.True(node.IsPrecisionChecked);
        
        _nodeRepoMock.Verify(r => r.UpdateAsync(node), Times.Once);
        _archipelagoMock.Verify(r => r.CheckLocationsAsync(sessionId, It.Is<long[]>(ids => ids.Contains(101L) && ids.Contains(102L))), Times.Once);
        // Should NOT trigger deterministic unlock
        _mockEngine.Verify(e => e.UnlockNextAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteDroneAsync_InSinglePlayerMode_ShouldCompleteChecksAndTriggerUnlock()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var session = new GameSession { Id = sessionId, Mode = "singleplayer" };
        var node = new MapNode { Id = nodeId, SessionId = sessionId };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);
        _nodeRepoMock.Setup(r => r.GetByIdAsync(nodeId)).ReturnsAsync(node);

        // Act
        var result = await _service.ExecuteDroneAsync(sessionId, nodeId);

        // Assert
        Assert.True(result);
        Assert.True(node.IsArrivalChecked);
        
        _nodeRepoMock.Verify(r => r.UpdateAsync(node), Times.Once);
        // Should trigger deterministic unlock
        _mockEngine.Verify(e => e.UnlockNextAsync(sessionId), Times.Once);
    }

    [Fact]
    public async Task ExecuteSignalAmplifierAsync_ShouldSetFlagOnSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new GameSession { Id = sessionId, SignalAmplifierActive = false };
        _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);

        // Act
        var result = await _service.ExecuteSignalAmplifierAsync(sessionId);

        // Assert
        Assert.True(result);
        Assert.True(session.SignalAmplifierActive);
        _sessionRepoMock.Verify(r => r.UpdateAsync(session), Times.Once);
    }

    [Fact]
    public async Task ExecuteDetourAsync_ShouldRelocateNodeAndSetFlag()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var originalLoc = new Point(-79.9, 40.4) { SRID = 4326 };
        var node = new MapNode { Id = nodeId, SessionId = sessionId, Location = originalLoc, RegionTag = "North" };
        var session = new GameSession { Id = sessionId, CenterLat = 40.4, CenterLon = -79.9, Radius = 5000 };

        _nodeRepoMock.Setup(r => r.GetByIdAsync(nodeId)).ReturnsAsync(node);
        _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);

        var discoveryPoints = new List<DiscoveryPoint> { new DiscoveryPoint(-79.91, 40.41) }; // This is North-ish
        _osmDiscoveryMock.Setup(r => r.GetRandomNodesAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<double>()))
            .ReturnsAsync(discoveryPoints);

        // Act
        var result = await _service.ExecuteDetourAsync(sessionId, nodeId);

        // Assert
        Assert.True(result);
        Assert.True(node.HasBeenRelocated);
        Assert.NotEqual(originalLoc.X, node.Location!.X);
        _nodeRepoMock.Verify(r => r.UpdateAsync(node), Times.Once);
    }
}
