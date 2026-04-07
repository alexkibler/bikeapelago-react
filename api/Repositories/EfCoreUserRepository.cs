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

    public async Task<User?> GetByUsernameOrEmailAsync(string identity)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == identity || u.Email == identity);
    }

    public async Task<User> CreateAsync(User user, string password)
    {
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
    
    public async Task<(string Token, User User)?> LoginAsync(string identity, string password)
    {
        var user = await GetByUsernameOrEmailAsync(identity);
        if (user == null || user.Password == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.Password)) return null;

        // Simplified mock token for compilation. Real logic should generate JWT.
        string fakeToken = $"jwt-token-{user.Id}";
        return (fakeToken, user);
    }

    public async Task<User?> GetCurrentUserAsync(string token)
    {
        Console.WriteLine($"DEBUG: Authenticating token: {token}");
        if (string.IsNullOrEmpty(token)) return null;

        if (token.StartsWith("jwt-token-"))
        {
            var idStr = token.Substring(10);
            if (Guid.TryParse(idStr, out var id))
            {
                var user = await GetByIdAsync(id);
                if (user == null) Console.WriteLine($"DEBUG: User not found for ID: {id}");
                return user;
            }
            Console.WriteLine($"DEBUG: Failed to parse GUID from: {idStr}");
        }
        else
        {
            Console.WriteLine("DEBUG: Token does not start with jwt-token-");
        }
        return null;
    }
}
