using Bikeapelago.Api.Data;
using Bikeapelago.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Bikeapelago.Api.Repositories;

public class EfCoreUserRepository(BikeapelagoDbContext context, IConfiguration configuration) : IUserRepository
{
    private readonly BikeapelagoDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;

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

        var token = GenerateJwt(user);
        return (token, user);
    }

    public async Task<User?> GetCurrentUserAsync(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var key = _configuration["Jwt:Key"] ?? "your-secret-key-at-least-32-chars-long";
        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = true,
            ValidIssuer = "bikeapelago-api",
            ValidateAudience = true,
            ValidAudience = "bikeapelago-frontend",
            ValidateLifetime = true,
        };

        try
        {
            var principal = handler.ValidateToken(token, validationParams, out _);
            var idClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(idClaim, out var id))
                return await GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: JWT validation failed: {ex.Message}");
        }

        return null;
    }

    private string GenerateJwt(User user)
    {
        var key = _configuration["Jwt:Key"] ?? "your-secret-key-at-least-32-chars-long";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username ?? ""),
        };

        var jwt = new JwtSecurityToken(
            issuer: "bikeapelago-api",
            audience: "bikeapelago-frontend",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
