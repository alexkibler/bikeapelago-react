using Microsoft.AspNetCore.Mvc;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Repositories;

namespace Bikeapelago.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserRepository userRepository) : ControllerBase
{
    private readonly IUserRepository _userRepository = userRepository;

    [HttpPost("login")]
    [HttpPost("login/")]
    [HttpPost("/api/pb/collections/users/auth-with-password")]
    [HttpPost("/api/pb/collections/users/auth-with-password/")]
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

    [HttpPost("register")]
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
