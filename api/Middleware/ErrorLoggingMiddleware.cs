using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Bikeapelago.Api.Data;
using Bikeapelago.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bikeapelago.Api.Middleware;

public class ErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorLoggingMiddleware> _logger;

    public ErrorLoggingMiddleware(RequestDelegate next, ILogger<ErrorLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Enable buffering so we can read the body multiple times if needed
        context.Request.EnableBuffering();

        try
        {
            await _next(context);

            // Log if status code is >= 400 (Client or Server Error)
            if (context.Response.StatusCode >= 400)
            {
                await LogErrorToDb(context, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during the request.");
            await LogErrorToDb(context, ex);
            throw; // Re-throw to allow default exception handling
        }
    }

    private async Task LogErrorToDb(HttpContext context, Exception? ex)
    {
        try
        {
            using var scope = context.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BikeapelagoDbContext>();

            var request = context.Request;
            
            // Read request body (safe to re-read because we used EnableBuffering)
            string? requestBody = null;
            if (request.ContentLength > 0)
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true);
                var fullBody = await reader.ReadToEndAsync();
                requestBody = fullBody.Length > 2000 ? fullBody.Substring(0, 2000) + "..." : fullBody;
                request.Body.Position = 0; // Reset for others
            }

            var authorizationHeader = request.Headers["Authorization"].ToString();
            var redactedAuthorization = string.IsNullOrEmpty(authorizationHeader) ? "" : "[REDACTED]";
            var apiLog = new ApiLog
            {
                Timestamp = DateTime.UtcNow,
                Method = request.Method,
                Path = request.Path,
                QueryString = request.QueryString.ToString(),
                StatusCode = context.Response.StatusCode,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = request.Headers["User-Agent"].ToString(),
                UserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User.FindFirst("id")?.Value,
                ExceptionType = ex?.GetType().Name,
                StackTrace = ex?.StackTrace,
                RequestBody = $"Request Header: {redactedAuthorization}\n\nBody: {requestBody}"
            };

            dbContext.ApiLogs.Add(apiLog);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception logEx)
        {
            // Fail silently so as not to crash the original request's error handling
            _logger.LogError(logEx, "Failed to log API error to database.");
        }
    }
}
