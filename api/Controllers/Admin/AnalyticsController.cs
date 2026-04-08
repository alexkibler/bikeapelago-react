using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bikeapelago.Api.Data;
using Bikeapelago.Api.Authorization;
using System.Linq;

namespace Bikeapelago.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/analytics")]
[AdminAuthorize]
public class AnalyticsController(BikeapelagoDbContext context) : ControllerBase
{
    private readonly BikeapelagoDbContext _context = context;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeSessions = await _context.GameSessions.CountAsync(s => s.Status == Models.SessionStatus.Active);
        var totalNodes = await _context.MapNodes.CountAsync();
        
        // Mock API utilization for now or calculate from logs
        var apiUtilization = 84; 

        return Ok(new
        {
            totalUsers,
            activeSessions,
            totalNodes,
            apiUtilization
        });
    }

    [HttpGet("traffic")]
    public async Task<IActionResult> GetTraffic()
    {
        var now = DateTime.UtcNow;
        var labels = new List<string>();
        var data = new List<int>();

        // Get last 24 hours traffic by hour
        for (int i = 23; i >= 0; i--)
        {
            var hour = now.AddHours(-i);
            var count = await _context.ApiLogs
                .CountAsync(l => l.Timestamp >= hour.AddHours(-1) && l.Timestamp < hour);
            
            labels.Add(hour.ToString("HH:00"));
            data.Add(count);
        }

        return Ok(new
        {
            labels,
            data
        });
    }
}
