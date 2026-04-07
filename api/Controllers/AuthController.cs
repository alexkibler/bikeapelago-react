using Microsoft.AspNetCore.Mvc;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;
using System.Text.Json.Serialization;

namespace Bikeapelago.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public AuthController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
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
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("weight")]
        public double? Weight { get; set; }
    }

    [HttpPatch("/api/users/{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound(new { message = "User not found." });

        if (request.Name != null) user.Name = request.Name;
        if (request.Weight.HasValue) user.Weight = request.Weight.Value;

        var updated = await _userRepository.UpdateAsync(user);
        return Ok(updated);
    }

    [HttpPost("/api/auth/register")]
    [HttpPost("/api/pb/collections/users/records")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try {
            var user = new User {
                Username = request.Username,
                Name = request.Name
            };
            var createdUser = await _userRepository.CreateAsync(user, request.Password);
            return Ok(createdUser);
        } catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }
}
