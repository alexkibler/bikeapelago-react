using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Bikeapelago.Api.Controllers;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Services;
using Microsoft.Extensions.Logging;

namespace Bikeapelago.Api.Tests.Unit;

public class IdorTests
{
    private SessionsController CreateControllerWithUser(Guid userId, out Mock<IGameSessionRepository> sessionRepoMock)
    {
        sessionRepoMock = new Mock<IGameSessionRepository>();
        var userRepoMock = new Mock<IUserRepository>();
        var nodeRepoMock = new Mock<IMapNodeRepository>();
        var mapboxMock = new Mock<IMapboxRoutingService>();
        // FitAnalysisService has dependencies, but we can pass null since we are only testing IDORs that happen before it's used.
        // Wait, FitAnalysisService is a concrete class. We might need to mock its dependencies or pass null if possible. Let's pass null and see.

        var user = new User { Id = userId, UserName = "attacker" };
        userRepoMock.Setup(r => r.GetCurrentUserAsync(It.IsAny<string>())).ReturnsAsync(user);

        var controller = new SessionsController(
            sessionRepoMock.Object,
            nodeRepoMock.Object,
            userRepoMock.Object,
            null!,
            mapboxMock.Object);

        // Mock HttpContext to have the auth header
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer dummy_token";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }

    [Fact]
    public async Task UpdateSession_ByDifferentUser_ShouldReturnForbid()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var controller = CreateControllerWithUser(attackerId, out var sessionRepoMock);

        var session = new GameSession { Id = sessionId, UserId = ownerId };
        sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);

        var request = new SessionsController.UpdateSessionRequest { ApServerUrl = "hacked" };

        var result = await controller.UpdateSession(sessionId, request);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetSession_ByDifferentUser_ShouldReturnForbid()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var controller = CreateControllerWithUser(attackerId, out var sessionRepoMock);

        var session = new GameSession { Id = sessionId, UserId = ownerId };
        sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);

        var result = await controller.GetSession(sessionId);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GenerateSessionNodes_ByDifferentUser_ShouldReturnForbid()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var controller = CreateControllerWithUser(attackerId, out var sessionRepoMock);

        var session = new GameSession { Id = sessionId, UserId = ownerId };
        sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);

        var nodeServiceMock = new Mock<NodeGenerationService>(
            new Mock<IOsmDiscoveryService>().Object,
            new Mock<IMapNodeRepository>().Object,
            sessionRepoMock.Object,
            new Mock<ILogger<NodeGenerationService>>().Object);

        var request = new NodeGenerationRequest();

        var result = await controller.GenerateSessionNodes(sessionId, request, nodeServiceMock.Object);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task RouteToAvailableNodes_ByDifferentUser_ShouldReturnForbid()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var controller = CreateControllerWithUser(attackerId, out var sessionRepoMock);

        var session = new GameSession { Id = sessionId, UserId = ownerId };
        sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId)).ReturnsAsync(session);

        var result = await controller.RouteToAvailableNodes(sessionId);

        Assert.IsType<ForbidResult>(result);
    }
}
