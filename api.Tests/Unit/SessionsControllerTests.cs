using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Bikeapelago.Api.Controllers;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Validators;

namespace Bikeapelago.Api.Tests.Unit;

public class SessionsControllerTests
{
    private readonly Mock<IGameSessionRepository> _sessionRepoMock;
    private readonly Mock<IMapNodeRepository> _nodeRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IFitAnalysisService> _fitAnalysisMock;
    private readonly Mock<IRouteBuilderService> _routeBuilderMock;
    private readonly Mock<IItemExecutionService> _itemExecutionMock;
    private readonly SessionsController _controller;
    private readonly Guid _userId;

    public SessionsControllerTests()
    {
        _sessionRepoMock = new Mock<IGameSessionRepository>();
        _nodeRepoMock = new Mock<IMapNodeRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _fitAnalysisMock = new Mock<IFitAnalysisService>();
        _routeBuilderMock = new Mock<IRouteBuilderService>();
        _itemExecutionMock = new Mock<IItemExecutionService>();

        _userId = Guid.NewGuid();

        _controller = new SessionsController(
            _sessionRepoMock.Object,
            _nodeRepoMock.Object,
            _userRepoMock.Object,
            _fitAnalysisMock.Object,
            Mock.Of<IProgressionEngineFactory>(),
            new SessionValidator(Mock.Of<ILogger<SessionValidator>>()),
            _routeBuilderMock.Object,
            _itemExecutionMock.Object);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private void SetupOwnedSession(Guid sessionId)
    {
        _sessionRepoMock.Setup(repo => repo.GetByIdAsync(sessionId))
            .ReturnsAsync(new GameSession { Id = sessionId, UserId = _userId, ConnectionMode = "singleplayer" });
    }

    [Fact]
    public async Task GetSessions_WithValidClaims_ReturnsOk()
    {
        // Arrange
        _sessionRepoMock.Setup(repo => repo.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<GameSession> { new GameSession { Id = Guid.NewGuid(), UserId = _userId } });

        // Act
        var result = await _controller.GetSessions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var sessions = Assert.IsAssignableFrom<IEnumerable<GameSession>>(okResult.Value);
        Assert.Single(sessions);
    }

    // ── RouteWaypoints (New Action) – delegation paths ──────────────────────────

    [Fact]
    public async Task RouteWaypoints_DelegatesToServiceAndReturnsOk()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        SetupOwnedSession(sessionId);
        var request = new RouteWaypointsRequest { NodeIds = [Guid.NewGuid()] };
        
        _routeBuilderMock.Setup(s => s.BuildRouteAsync(sessionId, request))
            .ReturnsAsync(new RouteBuilderResult
            {
                Success = true,
                TotalDistanceMeters = 1000,
                TotalDurationSeconds = 300,
                ElevationGain = 50,
                GpxString = "<gpx/>",
                Geometry = [[-79.9, 40.4]],
                OrderedNodeIds = request.NodeIds
            });

        // Act
        var result = await _controller.RouteWaypoints(sessionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic? val = okResult.Value;
        Assert.NotNull(val);
        Assert.True(val?.success);
        Assert.Equal(1000.0, (double)val?.totalDistanceMeters);
        _routeBuilderMock.Verify(s => s.BuildRouteAsync(sessionId, request), Times.Once);
    }

    [Fact]
    public async Task RouteWaypoints_ServiceReturnsSessionNotFound_ReturnsNotFound()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepoMock.Setup(repo => repo.GetByIdAsync(sessionId)).ReturnsAsync((GameSession?)null);

        var result = await _controller.RouteWaypoints(sessionId, new RouteWaypointsRequest());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RouteWaypoints_ServiceReturnsOtherError_ReturnsBadRequest()
    {
        var sessionId = Guid.NewGuid();
        SetupOwnedSession(sessionId);
        var request = new RouteWaypointsRequest();
        _routeBuilderMock.Setup(s => s.BuildRouteAsync(sessionId, request))
            .ReturnsAsync(RouteBuilderResult.Fail("No available nodes to route to"));

        var result = await _controller.RouteWaypoints(sessionId, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("No available nodes", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task RouteWaypoints_ServiceThrows_ReturnsInternalServerError()
    {
        var sessionId = Guid.NewGuid();
        SetupOwnedSession(sessionId);
        _routeBuilderMock.Setup(s => s.BuildRouteAsync(sessionId, It.IsAny<RouteWaypointsRequest>()))
            .ThrowsAsync(new Exception("Database explosion"));

        var result = await _controller.RouteWaypoints(sessionId, new RouteWaypointsRequest());

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task DeleteAllSessions_WithValidClaims_ReturnsNoContent()
    {
        // Arrange
        _sessionRepoMock.Setup(repo => repo.DeleteAllByUserIdAsync(_userId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteAllSessions();

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task AnalyzeFitFile_WhenTestUserDoesNotOwnSession_ReturnsForbid()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepoMock.Setup(repo => repo.GetByIdAsync(sessionId))
            .ReturnsAsync(new GameSession { Id = sessionId, UserId = Guid.NewGuid() });

        await using var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, stream.Length, "file", "ride.fit");

        var result = await _controller.AnalyzeFitFile(sessionId, file);

        Assert.IsType<ForbidResult>(result);
        _fitAnalysisMock.Verify(
            service => service.AnalyzeFitFile(
                It.IsAny<Stream>(),
                It.IsAny<IEnumerable<MapNode>>(),
                It.IsAny<double>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteDrone_WhenSessionBelongsToDifferentUser_ReturnsForbid()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepoMock.Setup(repo => repo.GetByIdAsync(sessionId))
            .ReturnsAsync(new GameSession { Id = sessionId, UserId = Guid.NewGuid() });

        var result = await _controller.ExecuteDrone(sessionId, Guid.NewGuid());

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DebugForceComplete_WhenSessionBelongsToDifferentUser_ReturnsForbid()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepoMock.Setup(repo => repo.GetByIdAsync(sessionId))
            .ReturnsAsync(new GameSession { Id = sessionId, UserId = Guid.NewGuid() });

        var result = await _controller.DebugForceComplete(sessionId);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task SetItemCount_WithNegativeCount_ReturnsBadRequest()
    {
        var result = await _controller.SetItemCount(
            Guid.NewGuid(),
            ItemDefinitions.Detour,
            -1,
            Mock.Of<IArchipelagoService>());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateSession_SetsAuthenticatedUserAndSafeDefaults()
    {
        // Arrange
        GameSession? captured = null;
        _sessionRepoMock.Setup(repo => repo.CreateAsync(It.IsAny<GameSession>()))
            .Callback<GameSession>(s => captured = s)
            .ReturnsAsync((GameSession s) => s);

        var request = new SessionsController.CreateSessionRequest
        {
            Name = "My Session",
            CenterLat = 40.44,
            CenterLon = -79.99,
            Radius = 5000,
            ConnectionMode = "archipelago"
        };

        // Act
        var result = await _controller.CreateSession(request);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
        Assert.NotNull(captured);
        Assert.Equal(_userId, captured!.UserId);
        Assert.Equal(SessionStatus.SetupInProgress, captured.Status);
        Assert.Equal("archipelago", captured.ConnectionMode);
        Assert.Equal(5000, captured.Radius);
        Assert.False(string.IsNullOrWhiteSpace(captured.CreatedAt));
        Assert.False(string.IsNullOrWhiteSpace(captured.UpdatedAt));
    }

    [Fact]
    public async Task CreateSession_WithOnlyOneCoordinate_ReturnsBadRequest()
    {
        var request = new SessionsController.CreateSessionRequest
        {
            CenterLat = 40.44
        };

        var result = await _controller.CreateSession(request);

        Assert.IsType<BadRequestObjectResult>(result);
        _sessionRepoMock.Verify(repo => repo.CreateAsync(It.IsAny<GameSession>()), Times.Never);
    }

    [Fact]
    public async Task RouteWaypoints_WhenSessionBelongsToDifferentUser_ReturnsForbid()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepoMock.Setup(repo => repo.GetByIdAsync(sessionId))
            .ReturnsAsync(new GameSession { Id = sessionId, UserId = Guid.NewGuid() });

        var result = await _controller.RouteWaypoints(sessionId, new RouteWaypointsRequest());

        Assert.IsType<ForbidResult>(result);
        _routeBuilderMock.Verify(s => s.BuildRouteAsync(It.IsAny<Guid>(), It.IsAny<RouteWaypointsRequest>()), Times.Never);
    }

    [Fact]
    public async Task GenerateSessionNodes_WithInvalidRadius_ReturnsBadRequest()
    {
        var sessionId = Guid.NewGuid();
        SetupOwnedSession(sessionId);
        var nodeGenerationService = new Mock<INodeGenerationService>();

        var result = await _controller.GenerateSessionNodes(sessionId, new NodeGenerationRequest
        {
            CenterLat = 40.44,
            CenterLon = -79.99,
            Radius = -1,
            NodeCount = 50,
            TransportMode = "bike",
            GameMode = "singleplayer"
        }, nodeGenerationService.Object);

        Assert.IsType<BadRequestObjectResult>(result);
        nodeGenerationService.Verify(s => s.GenerateNodesAsync(It.IsAny<NodeGenerationRequest>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteDetour_WithEmptyNodeId_ReturnsBadRequest()
    {
        var result = await _controller.ExecuteDetour(Guid.NewGuid(), Guid.Empty);

        Assert.IsType<BadRequestObjectResult>(result);
        _itemExecutionMock.Verify(s => s.ExecuteDetourAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteDrone_WithEmptyNodeId_ReturnsBadRequest()
    {
        var result = await _controller.ExecuteDrone(Guid.NewGuid(), Guid.Empty);

        Assert.IsType<BadRequestObjectResult>(result);
        _itemExecutionMock.Verify(s => s.ExecuteDroneAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GenerateSessionNodes_WhenSessionBelongsToDifferentUser_ReturnsForbid()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepoMock.Setup(repo => repo.GetByIdAsync(sessionId))
            .ReturnsAsync(new GameSession { Id = sessionId, UserId = Guid.NewGuid() });
        var nodeGenerationService = new Mock<INodeGenerationService>();

        var result = await _controller.GenerateSessionNodes(sessionId, new NodeGenerationRequest
        {
            CenterLat = 40.44,
            CenterLon = -79.99,
            Radius = 1000,
            NodeCount = 10,
            TransportMode = "bike",
            GameMode = "singleplayer"
        }, nodeGenerationService.Object);

        Assert.IsType<ForbidResult>(result);
        nodeGenerationService.Verify(s => s.GenerateNodesAsync(It.IsAny<NodeGenerationRequest>()), Times.Never);
    }

    [Fact]
    public async Task ValidateNodes_WithEmptyPoints_ReturnsBadRequest()
    {
        var discoveryService = new Mock<IOsmDiscoveryService>();

        var result = await _controller.ValidateNodes(discoveryService.Object, new ValidateRequest([], "bike"));

        Assert.IsType<BadRequestObjectResult>(result.Result);
        discoveryService.Verify(d => d.ValidateNodesAsync(It.IsAny<ValidateRequest>()), Times.Never);
    }

    [Fact]
    public async Task ValidateNodes_WithInvalidProfile_ReturnsBadRequest()
    {
        var discoveryService = new Mock<IOsmDiscoveryService>();

        var request = new ValidateRequest([new DiscoveryPoint(-79.99, 40.44)], "spaceship");
        var result = await _controller.ValidateNodes(discoveryService.Object, request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        discoveryService.Verify(d => d.ValidateNodesAsync(It.IsAny<ValidateRequest>()), Times.Never);
    }
}
