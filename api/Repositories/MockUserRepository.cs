using System.Threading.Tasks;
using Bikeapelago.Api.Models;

namespace Bikeapelago.Api.Repositories;

public class MockUserRepository : IUserRepository
{
    public Task<User?> GetByIdAsync(string id)
    {
        return Task.FromResult<User?>(new User { Id = "test-id", Username = "testuser", Name = "Test User" });
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        if (username == "testuser")
            return Task.FromResult<User?>(new User { Id = "test-id", Username = "testuser", Name = "Test User" });
        return Task.FromResult<User?>(null);
    }

    public Task<User> CreateAsync(User user, string password)
    {
        user.Id = Guid.NewGuid().ToString();
        return Task.FromResult(user);
    }

    public Task<User> UpdateAsync(User user)
    {
        return Task.FromResult(user);
    }

    public Task<(string Token, User User)?> LoginAsync(string username, string password)
    {
        if (username == "testuser" && password == "Password")
        {
            var user = new User { Id = "test-id", Username = "testuser", Name = "Test User" };
            return Task.FromResult<(string Token, User User)?>(("mock-jwt-token-for-e2e", user));
        }
        return Task.FromResult<(string Token, User User)?>(null);
    }

    public Task<User?> GetCurrentUserAsync(string token)
    {
        // In mock mode, any token resolves to the test user
        return Task.FromResult<User?>(new User { Id = "test-id", Username = "testuser", Name = "Test User" });
    }
}
