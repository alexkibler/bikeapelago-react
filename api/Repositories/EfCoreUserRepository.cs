using Bikeapelago.Api.Data;
using Bikeapelago.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bikeapelago.Api.Repositories;

public class EfCoreUserRepository(BikeapelagoDbContext context) : IUserRepository
{
    private readonly BikeapelagoDbContext _context = context;

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User> CreateAsync(User user, string password)
    {
        // For EF Core, you'll need to hash the password properly before inserting
        // Assuming user.Password is left as null and stored securely or managed here.
        user.Password = BCrypt.Net.BCrypt.HashPassword(password);
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<(string Token, User User)?> LoginAsync(string username, string password)
    {
        var user = await GetByUsernameAsync(username);
        if (user == null || user.Password == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.Password)) return null;

        // Simplified mock token for compilation. Real logic should generate JWT.
        string fakeToken = $"jwt-token-{user.Id}";
        return (fakeToken, user);
    }

    public async Task<User?> GetCurrentUserAsync(string token)
    {
        // Extract user id from JWT token in a real app.
        // Assuming token format contains user ID for this mock logic:
        if (token.StartsWith("jwt-token-") && Guid.TryParse(token.Substring(10), out var id))
        {
            return await GetByIdAsync(id);
        }
        return null;
    }
}
