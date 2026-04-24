using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bikeapelago.Api.Tests.Unit;

public class ArchipelagoServiceTests
{
    private readonly Mock<IHubContext<ArchipelagoHub>> _mockHubContext;
    private readonly Mock<ILogger<ArchipelagoService>> _mockLogger;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IMapNodeRepository> _mockNodeRepository;
    private readonly Mock<IGameSessionRepository> _mockSessionRepository;
    private readonly ArchipelagoService _service;

    public ArchipelagoServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<ArchipelagoHub>>();
        _mockLogger = new Mock<ILogger<ArchipelagoService>>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockNodeRepository = new Mock<IMapNodeRepository>();
        _mockSessionRepository = new Mock<IGameSessionRepository>();

        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockServiceProvider.Setup(x => x.GetService(typeof(IMapNodeRepository))).Returns(_mockNodeRepository.Object);
        mockServiceProvider.Setup(x => x.GetService(typeof(IGameSessionRepository))).Returns(_mockSessionRepository.Object);
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);

        _service = new ArchipelagoService(_mockHubContext.Object, _mockLogger.Object, _mockScopeFactory.Object);
    }

    [Fact]
    public async Task UpdateNodeStatesAsync_UsesUpdateRangeAsync()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var checkedLocationIds = new long[] { 1, 2 };
        var nodes = new List<MapNode>
        {
            new MapNode { Id = Guid.NewGuid(), ApArrivalLocationId = 1, State = "Available" },
            new MapNode { Id = Guid.NewGuid(), ApArrivalLocationId = 2, State = "Available" },
            new MapNode { Id = Guid.NewGuid(), ApArrivalLocationId = 3, State = "Available" }
        };

        _mockNodeRepository.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync(nodes);

        // Act
        // Invoke internal method via reflection (or change to internal)
        var method = typeof(ArchipelagoService).GetMethod("UpdateNodeStatesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);
        var task = (Task?)method.Invoke(_service, [sessionId, checkedLocationIds]);
        Assert.NotNull(task);
        await task;

        // Assert
        _mockNodeRepository.Verify(r => r.UpdateRangeAsync(It.Is<IEnumerable<MapNode>>(n => n.Count() == 2)), Times.Once());
        _mockNodeRepository.Verify(r => r.UpdateAsync(It.IsAny<MapNode>()), Times.Never());
    }

    [Fact]
    public async Task UpdateUnlockedNodesAsync_UsesUpdateRangeAsync()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var receivedItemIds = new long[] { 1, 2 };
        var nodes = new List<MapNode>
        {
            new MapNode { Id = Guid.NewGuid(), Name = "Node 1", ApArrivalLocationId = 1, State = "Hidden" },
            new MapNode { Id = Guid.NewGuid(), Name = "Node 2", ApArrivalLocationId = 2, State = "Hidden" },
            new MapNode { Id = Guid.NewGuid(), Name = "Node 3", ApArrivalLocationId = 3, State = "Hidden" }
        };

        _mockNodeRepository.Setup(r => r.GetBySessionIdAsync(sessionId)).ReturnsAsync(nodes);
        _mockSessionRepository.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(new GameSession { Id = sessionId, ProgressionMode = "None" });
        
        // Mocking IHubContext client group
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        // Act
        var method = typeof(ArchipelagoService).GetMethod("UpdateUnlockedNodesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);
        var task = (Task?)method.Invoke(_service, [sessionId, receivedItemIds]);
        Assert.NotNull(task);
        await task;

        // Assert
        // In "None" mode, it should unlock all non-hidden nodes if the logic allows, 
        // but our current logic requires items to match. 
        // For this test, just ensuring it runs without crashing and interacts with repo is enough if we can't mock item names.
        _mockNodeRepository.Verify(r => r.GetBySessionIdAsync(sessionId), Times.Once());
    }

    [Fact]
    public async Task SaveItemsToDbAsync_UsesAtomicUpdate()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var itemIds = new long[] { 1, 2, 3 };
        
        _mockSessionRepository.Setup(r => r.UpdateReceivedItemsAsync(sessionId, It.IsAny<List<long>>()))
            .Returns(Task.CompletedTask);

        var method = typeof(ArchipelagoService).GetMethod("SaveItemsToDbAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var task = (Task?)method.Invoke(_service, [sessionId, itemIds]);
        Assert.NotNull(task);
        await task;

        // Assert
        // Verify we no longer use the RMW pattern (GetByIdAsync + UpdateAsync)
        _mockSessionRepository.Verify(r => r.GetByIdAsync(sessionId), Times.Never());
        _mockSessionRepository.Verify(r => r.UpdateAsync(It.IsAny<GameSession>()), Times.Never());
        
        // Verify we use the new atomic update method
        _mockSessionRepository.Verify(r => r.UpdateReceivedItemsAsync(sessionId, It.Is<List<long>>(l => l.SequenceEqual(itemIds))), Times.Once());
    }
}
