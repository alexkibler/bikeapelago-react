using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Bikeapelago.Api.Models;
using Bikeapelago.Api.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Bikeapelago.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[AdminAuthorize]
public class UsersController(UserManager<User> userManager) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int perPage = 30)
    {
        var query = _userManager.Users;
        var total = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync();

        return Ok(new
        {
            page,
            perPage,
            totalItems = total,
            totalPages = (int)Math.Ceiling((double)total / perPage),
            items
        });
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Password cannot be empty" });

        // Remove existing password and add new one
        await _userManager.RemovePasswordAsync(user);
        
        var addResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
        if (!addResult.Succeeded)
            return BadRequest(new { message = string.Join(", ", addResult.Errors.Select(e => e.Description)) });

        return Ok(new { message = "Password reset successfully" });
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}
