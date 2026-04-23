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
    /// <summary>The subset of requested nodes that passed all validation rules.</summary>
    public List<MapNode> ValidNodes { get; init; } = [];

    /// <summary>Human-readable validation failure reason, or null if validation passed.</summary>
    public string? Error { get; init; }

    public bool IsValid => Error == null && ValidNodes.Count > 0;
}

/// <summary>
/// Validates session actions (e.g., node checking) against the current state.
/// </summary>
public class SessionValidator(ILogger<SessionValidator> logger)
{
    private readonly ILogger<SessionValidator> _logger = logger;

    public virtual SessionValidationResult ValidateNodeCheck(IEnumerable<MapNode> requestedNodes, Guid sessionId)
    {
        var nodes = requestedNodes.ToList();

        if (nodes.Count == 0)
            return Fail("No valid nodes found to check.");

        // Rule: node must be Available (not Hidden or already Checked)
        var unavailable = nodes.Where(n => n.State != "Available").ToList();
        if (unavailable.Count > 0)
        {
            var ids = string.Join(", ", unavailable.Select(n => n.Id));
            _logger.LogWarning(
                "Session {SessionId}: {Count} node(s) rejected — not in Available state: {Ids}",
                sessionId, unavailable.Count, ids);
        }

        var validNodes = nodes.Where(n => n.State == "Available").ToList();
        if (validNodes.Count == 0)
            return Fail("None of the submitted nodes are in an Available state.");

        // TODO: Validate that the uploaded .fit file's timestamp is > the generated .gpx file's timestamp.
        // Requires storing the GPX generation timestamp in the database first.

        return Ok(validNodes);
    }

    private static SessionValidationResult Ok(List<MapNode> nodes) => new() { ValidNodes = nodes };
    private static SessionValidationResult Fail(string error) => new() { Error = error };
}
