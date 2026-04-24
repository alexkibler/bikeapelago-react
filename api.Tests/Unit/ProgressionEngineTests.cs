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

namespace Bikeapelago.Api.Tests.Unit;

public class SinglePlayerProgressionEngineTests
{
    private readonly Mock<IMapNodeRepository> _mockNodeRepo;
    private readonly Mock<ILogger<SinglePlayerProgressionEngine>> _mockLogger;
    private readonly SinglePlayerProgressionEngine _engine;
    private readonly Guid _sessionId = Guid.NewGuid();

    public SinglePlayerProgressionEngineTests()
    {
        _mockNodeRepo = new Mock<IMapNodeRepository>();
        _mockLogger = new Mock<ILogger<SinglePlayerProgressionEngine>>();
        _engine = new SinglePlayerProgressionEngine(_mockNodeRepo.Object, _mockLogger.Object);
    }

    // --- CheckNodesAsync ---

    [Fact]
    public async Task CheckNodesAsync_MarksTargetNodesAsChecked()
    {
        // Arrange
        var nodes = new List<MapNode>
        {
            new() { Id = Guid.NewGuid(), SessionId = _sessionId, ApArrivalLocationId = 1, State = "Available" },
            new() { Id = Guid.NewGuid(), SessionId = _sessionId, ApArrivalLocationId = 2, State = "Available" },
        };
        // UnlockNextAsync will need to list all session nodes each call
        _mockNodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId)).ReturnsAsync([]);

        // Act
        await _engine.CheckNodesAsync(_sessionId, nodes);

        // Assert — all target nodes were saved as Checked in a single bulk call
        _mockNodeRepo.Verify(r => r.UpdateRangeAsync(
            It.Is<IEnumerable<MapNode>>(n => n.All(x => x.State == "Checked") && n.Count() == 2)),
            Times.Once());
    }

    [Fact]
    public async Task CheckNodesAsync_SkipsAlreadyCheckedNodes()
    {
        // Arrange
        var alreadyChecked = new MapNode { Id = Guid.NewGuid(), SessionId = _sessionId, ApArrivalLocationId = 1, State = "Checked" };
        var newNode = new MapNode { Id = Guid.NewGuid(), SessionId = _sessionId, ApArrivalLocationId = 2, State = "Available" };

        _mockNodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId)).ReturnsAsync([]);

        // Act
        await _engine.CheckNodesAsync(_sessionId, [alreadyChecked, newNode]);

        // Assert — only the newly-checked node goes into UpdateRangeAsync
        _mockNodeRepo.Verify(r => r.UpdateRangeAsync(
            It.Is<IEnumerable<MapNode>>(n => n.Count() == 1 && n.First().ApArrivalLocationId == 2)),
            Times.Once());
    }

    [Fact]
    public async Task CheckNodesAsync_DoesNothing_WhenAllNodesAlreadyChecked()
    {
        // Arrange
        var nodes = new List<MapNode>
        {
            new() { Id = Guid.NewGuid(), SessionId = _sessionId, ApArrivalLocationId = 1, State = "Checked" },
        };

        // Act
        await _engine.CheckNodesAsync(_sessionId, nodes);

        // Assert — no DB writes, no unlock triggered
        _mockNodeRepo.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<MapNode>>()), Times.Never());
        _mockNodeRepo.Verify(r => r.GetBySessionIdAsync(_sessionId), Times.Never());
    }

    [Fact]
    public async Task CheckNodesAsync_CallsUnlockNextOncePerNewlyCheckedNode()
    {
        // Arrange — 2 newly checked nodes → UnlockNextAsync should be called twice
        var nodes = new List<MapNode>
        {
            new() { Id = Guid.NewGuid(), SessionId = _sessionId, ApArrivalLocationId = 1, State = "Available" },
            new() { Id = Guid.NewGuid(), SessionId = _sessionId, ApArrivalLocationId = 2, State = "Available" },
        };

        // Each UnlockNextAsync call fetches nodes; return empty so it no-ops cleanly
        _mockNodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId)).ReturnsAsync([]);

        // Act
        await _engine.CheckNodesAsync(_sessionId, nodes);

        // Assert — GetBySessionIdAsync (used by UnlockNextAsync) called twice
        _mockNodeRepo.Verify(r => r.GetBySessionIdAsync(_sessionId), Times.Exactly(2));
    }

    // --- UnlockNextAsync ---

    [Fact]
    public async Task UnlockNextAsync_UnlocksLowestApArrivalLocationIdHiddenNode()
    {
        // Arrange
        var hidden1 = new MapNode { Id = Guid.NewGuid(), SessionId = _sessionId, ApArrivalLocationId = 802, State = "Hidden" };
        var hidden2 = new MapNode { Id = Guid.NewGuid(), SessionId = _sessionId, ApArrivalLocationId = 801, State = "Hidden" };
        var available = new MapNode { Id = Guid.NewGuid(), SessionId = _sessionId, ApArrivalLocationId = 800, State = "Available" };

        _mockNodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId))
            .ReturnsAsync([hidden1, hidden2, available]);

        // Act
        await _engine.UnlockNextAsync(_sessionId);

        // Assert — only the node with the lowest ApArrivalLocationId (801) is updated
        _mockNodeRepo.Verify(r => r.UpdateAsync(It.Is<MapNode>(n => n.ApArrivalLocationId == 801 && n.State == "Available")), Times.Once());
        _mockNodeRepo.Verify(r => r.UpdateAsync(It.Is<MapNode>(n => n.ApArrivalLocationId == 802)), Times.Never());
    }

    [Fact]
    public async Task UnlockNextAsync_DoesNothing_WhenNoHiddenNodes()
    {
        // Arrange
        _mockNodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId))
            .ReturnsAsync([new MapNode { State = "Available" }]);

        // Act
        await _engine.UnlockNextAsync(_sessionId);

        // Assert
        _mockNodeRepo.Verify(r => r.UpdateAsync(It.IsAny<MapNode>()), Times.Never());
    }
}

public class ArchipelagoProgressionEngineTests
{
    private readonly Mock<IArchipelagoService> _mockApService;
    private readonly Mock<IMapNodeRepository> _mockNodeRepo;
    private readonly Mock<ILogger<ArchipelagoProgressionEngine>> _mockLogger;
    private readonly ArchipelagoProgressionEngine _engine;
    private readonly Guid _sessionId = Guid.NewGuid();

    public ArchipelagoProgressionEngineTests()
    {
        _mockApService = new Mock<IArchipelagoService>();

        _mockNodeRepo = new Mock<IMapNodeRepository>();
        _mockLogger = new Mock<ILogger<ArchipelagoProgressionEngine>>();

        _engine = new ArchipelagoProgressionEngine(_mockApService.Object, _mockNodeRepo.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CheckNodesAsync_SendsAllLocationIdsToArchipelago()
    {
        // Arrange
        var nodes = new List<MapNode>
        {
            new() { Id = Guid.NewGuid(), ApArrivalLocationId = 800001, ApPrecisionLocationId = 800002, IsArrivalChecked = true, IsPrecisionChecked = true },
            new() { Id = Guid.NewGuid(), ApArrivalLocationId = 800003, ApPrecisionLocationId = 800004, IsArrivalChecked = true },
        };

        _mockApService.Setup(s => s.CheckLocationsAsync(_sessionId, It.IsAny<long[]>()))
            .Returns(Task.CompletedTask);

        // Act
        await _engine.CheckNodesAsync(_sessionId, nodes);

        // Assert
        _mockApService.Verify(s => s.CheckLocationsAsync(
            _sessionId,
            It.Is<long[]>(ids => ids.OrderBy(x => x).SequenceEqual(new long[] { 800001, 800002, 800003 }))),
            Times.Once());
    }

    [Fact]
    public async Task UnlockNextAsync_SendsCheckedLocationsToArchipelago()
    {
        // Arrange
        var nodes = new List<MapNode>
        {
            new() { Id = Guid.NewGuid(), ApArrivalLocationId = 800001, IsArrivalChecked = true },
            new() { Id = Guid.NewGuid(), ApArrivalLocationId = 800003, IsArrivalChecked = false },
        };

        _mockNodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId)).ReturnsAsync(nodes);
        _mockApService.Setup(s => s.CheckLocationsAsync(_sessionId, It.IsAny<long[]>()))
            .Returns(Task.CompletedTask);

        // Act
        await _engine.UnlockNextAsync(_sessionId);

        // Assert — only the Checked node's location is sent
        _mockApService.Verify(s => s.CheckLocationsAsync(
            _sessionId,
            It.Is<long[]>(ids => ids.SequenceEqual(new long[] { 800001 }))),
            Times.Once());
    }

    [Fact]
    public async Task UnlockNextAsync_DoesNotCallArchipelago_WhenNoCheckedNodes()
    {
        // Arrange
        var nodes = new List<MapNode>
        {
            new() { ApArrivalLocationId = 800001, State = "Available" },
        };

        _mockNodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId)).ReturnsAsync(nodes);

        // Act
        await _engine.UnlockNextAsync(_sessionId);

        // Assert
        _mockApService.Verify(s => s.CheckLocationsAsync(It.IsAny<Guid>(), It.IsAny<long[]>()), Times.Never());
    }
}
