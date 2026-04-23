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
using NetTopologySuite.Geometries;

namespace Bikeapelago.Api.Tests.Unit;

public class SessionsControllerTests
{
    private readonly Mock<IGameSessionRepository> _sessionRepoMock;
    private readonly Mock<IMapNodeRepository> _nodeRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IRouteBuilderService> _routeBuilderMock;
    private readonly SessionsController _controller;
    private readonly Guid _userId;

    public SessionsControllerTests()
    {
        _sessionRepoMock = new Mock<IGameSessionRepository>();
        _nodeRepoMock = new Mock<IMapNodeRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _routeBuilderMock = new Mock<IRouteBuilderService>();

        _userId = Guid.NewGuid();

        _controller = new SessionsController(
            _sessionRepoMock.Object,
            _nodeRepoMock.Object,
            _userRepoMock.Object,
            null!, // IFitAnalysisService
            Mock.Of<IProgressionEngineFactory>(),
            new SessionValidator(Mock.Of<ILogger<SessionValidator>>()),
            _routeBuilderMock.Object);

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
        var request = new RouteWaypointsRequest();
        _routeBuilderMock.Setup(s => s.BuildRouteAsync(sessionId, request))
            .ReturnsAsync(RouteBuilderResult.Fail("Session not found"));

        var result = await _controller.RouteWaypoints(sessionId, request);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RouteWaypoints_ServiceReturnsOtherError_ReturnsBadRequest()
    {
        var sessionId = Guid.NewGuid();
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
}
