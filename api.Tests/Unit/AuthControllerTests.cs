using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Bikeapelago.Api.Controllers;
using Bikeapelago.Api.Repositories;
using Bikeapelago.Api.Models;
using static Bikeapelago.Api.Controllers.AuthController;

namespace Bikeapelago.Api.Tests.Unit;

public class AuthControllerTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
    private readonly AuthController _controller;
    private readonly Guid _userId;

    public AuthControllerTests()
    {
        _userRepoMock = new Mock<IUserRepository>();

        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(roleStore.Object, null!, null!, null!, null!);

        _userId = Guid.NewGuid();

        _controller = new AuthController(
            _userRepoMock.Object,
            _userManagerMock.Object,
            _roleManagerMock.Object);

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
    public async Task UpdateUser_WithValidClaims_ReturnsOk()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            Name = "New Name"
        };

        var user = new User { Id = _userId, UserName = "testuser", Name = "Old Name" };

        _userRepoMock.Setup(repo => repo.GetByIdAsync(_userId)).ReturnsAsync(user);
        _userManagerMock.Setup(um => um.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.UpdateUser(_userId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedUser = Assert.IsType<User>(okResult.Value);
        Assert.Equal("New Name", updatedUser.Name);
    }

    [Fact]
    public async Task Login_WithMissingCredentials_ReturnsBadRequest()
    {
        var result = await _controller.Login(new LoginRequest
        {
            Username = " ",
            Password = ""
        });

        Assert.IsType<BadRequestObjectResult>(result);
        _userRepoMock.Verify(repo => repo.LoginAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Register_WithMissingCredentials_ReturnsBadRequest()
    {
        var result = await _controller.Register(new RegisterRequest
        {
            Username = "",
            Password = ""
        });

        Assert.IsType<BadRequestObjectResult>(result);
        _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUser_WithNonPositiveWeight_ReturnsBadRequest()
    {
        var user = new User { Id = _userId, UserName = "testuser", Name = "Old Name" };
        _userRepoMock.Setup(repo => repo.GetByIdAsync(_userId)).ReturnsAsync(user);

        var result = await _controller.UpdateUser(_userId, new UpdateUserRequest
        {
            Weight = 0
        });

        Assert.IsType<BadRequestObjectResult>(result);
        _userManagerMock.Verify(um => um.UpdateAsync(It.IsAny<User>()), Times.Never);
    }
}
