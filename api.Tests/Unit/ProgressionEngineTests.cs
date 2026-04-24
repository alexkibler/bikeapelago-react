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
    private readonly Mock<IGameSessionRepository> _mockSessionRepo;
    private readonly Mock<ILogger<SinglePlayerProgressionEngine>> _mockLogger;
    private readonly SinglePlayerProgressionEngine _engine;
    private readonly Guid _sessionId = Guid.NewGuid();

    public SinglePlayerProgressionEngineTests()
    {
        _mockNodeRepo = new Mock<IMapNodeRepository>();
        _mockSessionRepo = new Mock<IGameSessionRepository>();
        _mockLogger = new Mock<ILogger<SinglePlayerProgressionEngine>>();
        
        _mockSessionRepo.Setup(r => r.GetByIdAsync(_sessionId)).ReturnsAsync(new GameSession { Id = _sessionId });
        
        _engine = new SinglePlayerProgressionEngine(_mockNodeRepo.Object, _mockSessionRepo.Object, Mock.Of<IArchipelagoService>(), _mockLogger.Object);
    }

    // --- CheckNodesAsync ---

    [Fact]
    public async Task CheckNodesAsync_MarksTargetNodesAsChecked()
    {
        // Arrange
        var nodes = new List<NewlyCheckedNode>
        {
            new() { Id = Guid.NewGuid(), ApArrivalLocationId = 1, ArrivalChecked = true, PrecisionChecked = true },
            new() { Id = Guid.NewGuid(), ApArrivalLocationId = 2, ArrivalChecked = true, PrecisionChecked = true },
        };
        // It fetches nodes to check their current state and to unlock later
        _mockNodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId)).ReturnsAsync(nodes.Select(n => new MapNode { Id = n.Id, State = "Available" }).ToList());

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
        var alreadyCheckedId = Guid.NewGuid();
        var newNodeId = Guid.NewGuid();
        
        var alreadyCheckedNode = new MapNode { Id = alreadyCheckedId, SessionId = _sessionId, ApArrivalLocationId = 1, State = "Checked", IsArrivalChecked = true, IsPrecisionChecked = true };
        var newNode = new MapNode { Id = newNodeId, SessionId = _sessionId, ApArrivalLocationId = 2, State = "Available" };
        
        var nodes = new List<NewlyCheckedNode>
        {
            new() { Id = alreadyCheckedId, ApArrivalLocationId = 1, ArrivalChecked = true, PrecisionChecked = true },
            new() { Id = newNodeId, ApArrivalLocationId = 2, ArrivalChecked = true, PrecisionChecked = true },
        };

        _mockNodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId)).ReturnsAsync(new List<MapNode> { alreadyCheckedNode, newNode });

        // Act
        await _engine.CheckNodesAsync(_sessionId, nodes);

        // Assert — only the newly-checked node goes into UpdateRangeAsync
        _mockNodeRepo.Verify(r => r.UpdateRangeAsync(
            It.Is<IEnumerable<MapNode>>(n => n.Count() == 1 && n.First().ApArrivalLocationId == 2)),
            Times.Once());
    }

    [Fact]
    public async Task CheckNodesAsync_DoesNothing_WhenAllNodesAlreadyChecked()
    {
        // Arrange
        var alreadyCheckedId = Guid.NewGuid();
        var nodes = new List<NewlyCheckedNode>
        {
            new() { Id = alreadyCheckedId, ApArrivalLocationId = 1, ArrivalChecked = true, PrecisionChecked = true },
        };
        
        var dbNodes = new List<MapNode>
        {
            new() { Id = alreadyCheckedId, SessionId = _sessionId, ApArrivalLocationId = 1, State = "Checked", IsArrivalChecked = true, IsPrecisionChecked = true }
        };

        _mockNodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId)).ReturnsAsync(dbNodes);

        // Act
        await _engine.CheckNodesAsync(_sessionId, nodes);

        // Assert — no DB writes
        _mockNodeRepo.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<MapNode>>()), Times.Never());
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
        var nodes = new List<NewlyCheckedNode>
        {
            new() { Id = Guid.NewGuid(), ApArrivalLocationId = 800001, ApPrecisionLocationId = 800002, ArrivalChecked = true, PrecisionChecked = true },
            new() { Id = Guid.NewGuid(), ApArrivalLocationId = 800003, ApPrecisionLocationId = 800004, ArrivalChecked = true },
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
}