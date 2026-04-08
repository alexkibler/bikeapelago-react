using Bikeapelago.Api.Data;
using Bikeapelago.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Bikeapelago.Api.Repositories;

public class EfCoreUserRepository(BikeapelagoDbContext context, IConfiguration configuration, UserManager<User> userManager) : IUserRepository
{
    private readonly BikeapelagoDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;
    private readonly UserManager<User> _userManager = userManager;

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _userManager.FindByIdAsync(id.ToString());
    }

    public async Task<User?> GetByUsernameOrEmailAsync(string identity)
    {
        var user = await _userManager.FindByNameAsync(identity);
        if (user == null) user = await _userManager.FindByEmailAsync(identity);
        return user;
    }

    public async Task<User> CreateAsync(User user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded) 
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        return user;
    }
    
    public async Task<(string Token, User User)?> LoginAsync(string identity, string password)
    {
        var user = await GetByUsernameOrEmailAsync(identity);
        if (user == null) return null;

        var result = await _userManager.CheckPasswordAsync(user, password);
        if (!result) return null;

        var token = await GenerateJwt(user);
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

    private async Task<string> GenerateJwt(User user)
    {
        var key = _configuration["Jwt:Key"] ?? "your-secret-key-at-least-32-chars-long";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var jwt = new JwtSecurityToken(
            issuer: "bikeapelago-api",
            audience: "bikeapelago-frontend",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
