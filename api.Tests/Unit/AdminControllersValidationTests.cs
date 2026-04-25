using Bikeapelago.Api.Controllers.Admin;
using Bikeapelago.Api.Data;
using Bikeapelago.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Bikeapelago.Api.Tests.Unit;

public class AdminControllersValidationTests
{
    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public async Task UsersController_GetUsers_WithInvalidPaging_ReturnsBadRequest(int page, int perPage)
    {
        var userManager = CreateUserManagerMock();
        var controller = new UsersController(userManager.Object);

        var result = await controller.GetUsers(page, perPage);

        Assert.IsType<BadRequestObjectResult>(result);
        userManager.VerifyGet(um => um.Users, Times.Never);
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public async Task GenericDataController_GetList_WithInvalidPaging_ReturnsBadRequest(int page, int perPage)
    {
        var options = new DbContextOptionsBuilder<BikeapelagoDbContext>().Options;
        await using var context = new BikeapelagoDbContext(options);
        var controller = new GenericDataController(context);

        var result = await controller.GetList("users", page, perPage);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
