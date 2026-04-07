using System.Threading.Tasks;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Repositories;

public class MockUserRepository : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id)
    {
        return Task.FromResult<User?>(new User { Id = id, Username = "testuser", Name = "Test User" });
    }

    public Task<User?> GetByUsernameOrEmailAsync(string identity)
    {
        if (identity == "testuser" || identity == "test@example.com")
            return Task.FromResult<User?>(new User { Id = Guid.Empty, Username = "testuser", Name = "Test User", Email = "test@example.com" });
        return Task.FromResult<User?>(null);
    }

    public Task<User> CreateAsync(User user, string password)
    {
        user.Id = Guid.NewGuid();
        return Task.FromResult(user);
    }

    public Task<User> UpdateAsync(User user)
    {
        return Task.FromResult(user);
    }

    public Task<(string Token, User User)?> LoginAsync(string identity, string password)
    {
        if ((identity == "testuser" || identity == "test@example.com") && password == "Password")
        {
            var user = new User { Id = Guid.Empty, Username = "testuser", Name = "Test User" };
            return Task.FromResult<(string Token, User User)?>(("mock-jwt-token-for-e2e", user));
        }
        return Task.FromResult<(string Token, User User)?>(null);
    }

    public Task<User?> GetCurrentUserAsync(string token)
    {
        // In mock mode, any token resolves to the test user
        return Task.FromResult<User?>(new User { Id = Guid.Empty, Username = "testuser", Name = "Test User" });
    }
}
