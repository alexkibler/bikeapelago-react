using System;
using System.Collections.Generic;
using System.Linq;
using Bikeapelago.Api.Models;
using Microsoft.Extensions.Logging;

namespace Bikeapelago.Api.Validators;

/// <summary>
/// Result of a session/node validation check.
/// </summary>
public class SessionValidationResult
{
    public List<NewlyCheckedNode> ValidNodes { get; init; } = [];
    public string? Error { get; init; }
    public bool IsValid => Error == null && ValidNodes.Count > 0;
}

public class SessionValidator(ILogger<SessionValidator> logger)
{
    private readonly ILogger<SessionValidator> _logger = logger;

    public virtual SessionValidationResult ValidateNodeCheck(IEnumerable<MapNode> dbNodes, IEnumerable<NewlyCheckedNode> requestedChecks, Guid sessionId)
    {
        var checks = requestedChecks.ToList();
        var nodes = dbNodes.ToList();

        if (checks.Count == 0)
            return Fail("No valid nodes found to check.");

        var validChecks = new List<NewlyCheckedNode>();
        var rejectedIds = new List<Guid>();

        foreach (var check in checks)
        {
            var node = nodes.FirstOrDefault(n => n.Id == check.Id);
            if (node == null || node.State != "Available")
            {
                rejectedIds.Add(check.Id);
                continue;
            }
            validChecks.Add(check);
        }

        if (rejectedIds.Count > 0)
        {
            _logger.LogWarning(
                "Session {SessionId}: {Count} node(s) rejected — not found or not in Available state: {Ids}",
                sessionId, rejectedIds.Count, string.Join(", ", rejectedIds));
        }

        if (validChecks.Count == 0)
            return Fail("None of the submitted nodes are in an Available state.");

        return Ok(validChecks);
    }

    private static SessionValidationResult Ok(List<NewlyCheckedNode> checks) => new() { ValidNodes = checks };
    private static SessionValidationResult Fail(string error) => new() { Error = error };
}
