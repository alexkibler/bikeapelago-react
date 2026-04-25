using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Bikeapelago.Api.Controllers;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using Bikeapelago.Api.Validators;
using System.Security.Claims;

namespace Bikeapelago.Api.Tests.Unit;

public class SessionsControllerNodesTests
{
    private readonly Mock<IMapNodeRepository> _nodeRepo;
    private readonly Mock<IGameSessionRepository> _sessionRepo;
    private readonly Mock<IProgressionEngineFactory> _engineFactory;
    private readonly Mock<IProgressionEngine> _engine;
    private readonly Mock<ILogger<SessionsController>> _logger;
    private readonly SessionValidator _validator;
    private readonly SessionsController _controller;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();

    public SessionsControllerNodesTests()
    {
        _nodeRepo = new Mock<IMapNodeRepository>();
        _sessionRepo = new Mock<IGameSessionRepository>();
        _engineFactory = new Mock<IProgressionEngineFactory>();
        _engine = new Mock<IProgressionEngine>();
        _logger = new Mock<ILogger<SessionsController>>();

        _validator = new SessionValidator(Mock.Of<ILogger<SessionValidator>>());

        _engineFactory.Setup(f => f.CreateEngine(It.IsAny<string>())).Returns(_engine.Object);
        _engine.Setup(e => e.CheckNodesAsync(It.IsAny<Guid>(), It.IsAny<List<NewlyCheckedNode>>()))
            .Returns(Task.CompletedTask);

        _controller = new SessionsController(
            _sessionRepo.Object,
            _nodeRepo.Object,
            Mock.Of<IUserRepository>(),
            null!, // IFitAnalysisService
            _engineFactory.Object,
            _validator,
            null!, // IRouteBuilderService
            Mock.Of<IItemExecutionService>());

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private void SetupSession(string mode = "singleplayer") =>
        _sessionRepo.Setup(r => r.GetByIdAsync(_sessionId))
            .ReturnsAsync(new GameSession { Id = _sessionId, ConnectionMode = mode, UserId = _userId });

    // --- State validation ---

    [Fact]
    public async Task CheckNodes_RejectsRequest_WhenAllSubmittedNodesAreHidden()
    {
        SetupSession();
        var hiddenId = Guid.NewGuid();
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId)).ReturnsAsync(
        [
            new MapNode { Id = hiddenId, State = "Hidden" }
        ]);

        var result = await _controller.CheckNodes(_sessionId, new SessionsController.CheckNodesRequest
        {
            NodeIds = [hiddenId]
        });

        Assert.IsType<UnprocessableEntityObjectResult>(result);
        _engine.Verify(e => e.CheckNodesAsync(It.IsAny<Guid>(), It.IsAny<List<NewlyCheckedNode>>()), Times.Never());
    }

    [Fact]
    public async Task CheckNodes_RejectsRequest_WhenAllSubmittedNodesAreAlreadyChecked()
    {
        SetupSession();
        var checkedId = Guid.NewGuid();
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId)).ReturnsAsync(
        [
            new MapNode { Id = checkedId, State = "Checked" }
        ]);

        var result = await _controller.CheckNodes(_sessionId, new SessionsController.CheckNodesRequest
        {
            NodeIds = [checkedId]
        });

        Assert.IsType<UnprocessableEntityObjectResult>(result);
        _engine.Verify(e => e.CheckNodesAsync(It.IsAny<Guid>(), It.IsAny<List<NewlyCheckedNode>>()), Times.Never());
    }

    [Fact]
    public async Task CheckNodes_OnlyPassesAvailableNodes_ToEngine_WhenMixedStatesSubmitted()
    {
        SetupSession();
        var availableId = Guid.NewGuid();
        var hiddenId = Guid.NewGuid();
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId)).ReturnsAsync(
        [
            new MapNode { Id = availableId, State = "Available" },
            new MapNode { Id = hiddenId,    State = "Hidden" }
        ]);

        await _controller.CheckNodes(_sessionId, new SessionsController.CheckNodesRequest
        {
            NodeIds = [availableId, hiddenId]
        });

        // Engine receives only the Available node
        _engine.Verify(e => e.CheckNodesAsync(
            _sessionId,
            It.Is<List<NewlyCheckedNode>>(nodes => nodes.Count == 1 && nodes[0].Id == availableId)),
            Times.Once());
    }

    [Fact]
    public async Task CheckNodes_PassesAllAvailableNodes_WhenAllAreAvailable()
    {
        SetupSession();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId)).ReturnsAsync(
        [
            new MapNode { Id = id1, State = "Available" },
            new MapNode { Id = id2, State = "Available" }
        ]);

        var result = await _controller.CheckNodes(_sessionId, new SessionsController.CheckNodesRequest
        {
            NodeIds = [id1, id2]
        });

        Assert.IsType<AcceptedResult>(result);
        _engine.Verify(e => e.CheckNodesAsync(
            _sessionId,
            It.Is<List<NewlyCheckedNode>>(nodes => nodes.Count == 2)),
            Times.Once());
    }

    // --- Other guard cases ---

    [Fact]
    public async Task CheckNodes_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        _sessionRepo.Setup(r => r.GetByIdAsync(_sessionId)).ReturnsAsync((GameSession?)null);

        var result = await _controller.CheckNodes(_sessionId, new SessionsController.CheckNodesRequest
        {
            NodeIds = [Guid.NewGuid()]
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CheckNodes_ReturnsBadRequest_WhenNoMatchingNodesFound()
    {
        SetupSession();
        _nodeRepo.Setup(r => r.GetBySessionIdAsync(_sessionId)).ReturnsAsync([]);

        var result = await _controller.CheckNodes(_sessionId, new SessionsController.CheckNodesRequest
        {
            NodeIds = [Guid.NewGuid()]
        });

        Assert.IsType<UnprocessableEntityObjectResult>(result);
    }

    [Fact]
    public async Task CheckNodes_WhenSessionBelongsToDifferentUser_ReturnsForbid()
    {
        _sessionRepo.Setup(r => r.GetByIdAsync(_sessionId))
            .ReturnsAsync(new GameSession { Id = _sessionId, ConnectionMode = "singleplayer", UserId = Guid.NewGuid() });

        var result = await _controller.CheckNodes(_sessionId, new SessionsController.CheckNodesRequest
        {
            NodeIds = [Guid.NewGuid()]
        });

        Assert.IsType<ForbidResult>(result);
        _engine.Verify(e => e.CheckNodesAsync(It.IsAny<Guid>(), It.IsAny<List<NewlyCheckedNode>>()), Times.Never());
    }

    [Fact]
    public async Task GetSessionNodes_WhenSessionBelongsToDifferentUser_ReturnsForbid()
    {
        _sessionRepo.Setup(r => r.GetByIdAsync(_sessionId))
            .ReturnsAsync(new GameSession { Id = _sessionId, UserId = Guid.NewGuid() });

        var result = await _controller.GetSessionNodes(_sessionId);

        Assert.IsType<ForbidResult>(result);
        _nodeRepo.Verify(r => r.GetBySessionIdAsync(It.IsAny<Guid>()), Times.Never);
    }
}
