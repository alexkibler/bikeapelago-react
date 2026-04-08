using Microsoft.AspNetCore.Mvc;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace Bikeapelago.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public AuthController(IUserRepository userRepository, UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpPost("/api/auth/login")]
    [HttpPost("/api/pb/collections/users/auth-with-password")]
    [HttpPost("/api/pb/api/collections/users/auth-with-password")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _userRepository.LoginAsync(request.Username, request.Password);
        if (result != null)
        {
            return Ok(new {
                token = result.Value.Token,
                record = result.Value.User
            });
        }
        return Unauthorized(new { message = "Invalid credentials" });
    }

    public class UpdateUserRequest
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("weight")]
        public double? Weight { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }
    }

    [HttpPatch("/api/users/{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Unauthorized(new { message = "No auth token provided" });

        var token = authHeader["Bearer ".Length..].Trim();
        var currentUser = await _userRepository.GetCurrentUserAsync(token);
        if (currentUser == null || currentUser.Id != id)
            return Unauthorized(new { message = "Unauthorized to update this user." });

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound(new { message = "User not found." });

        if (request.Username != null) user.UserName = request.Username;
        if (request.Name != null) user.Name = request.Name;
        if (request.Weight.HasValue) user.Weight = request.Weight.Value;
        
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        if (request.Password != null)
        {
            await _userManager.RemovePasswordAsync(user);
            var addResult = await _userManager.AddPasswordAsync(user, request.Password);
            if (!addResult.Succeeded) return BadRequest(new { message = string.Join(", ", addResult.Errors.Select(e => e.Description)) });
        }

        return Ok(user);
    }

    [HttpPost("/api/auth/register")]
    [HttpPost("/api/pb/collections/users/records")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try {
            var user = new User {
                UserName = request.Username,
                Email = request.Username.Contains("@") ? request.Username : null,
                Name = request.Name
            };
            
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded) return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

            await _userManager.AddToRoleAsync(user, "User");
            
            return Ok(user);
        } catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }
}
