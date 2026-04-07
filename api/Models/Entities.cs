using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Bikeapelago.Api.Models;

public class User
{
    [Key]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("weight")]
    public double Weight { get; set; } = 75.0;

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonIgnore]
    public string? Password { get; set; }
}

public class GameSession
{
    [Key]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("user")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("ap_seed_name")]
    public string? ApSeedName { get; set; }

    [JsonPropertyName("ap_server_url")]
    public string? ApServerUrl { get; set; }

    [JsonPropertyName("ap_slot_name")]
    public string? ApSlotName { get; set; }

    [JsonPropertyName("center_lat")]
    public double? CenterLat { get; set; }

    [JsonPropertyName("center_lon")]
    public double? CenterLon { get; set; }

    [JsonPropertyName("radius")]
    public int? Radius { get; set; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SessionStatus Status { get; set; } = SessionStatus.SetupInProgress;

    [JsonPropertyName("created")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("updated")]
    public string UpdatedAt { get; set; } = string.Empty;
}

public class MapNode
{
    [Key]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("session")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("ap_location_id")]
    public int ApLocationId { get; set; }

    [JsonPropertyName("osm_node_id")]
    public string OsmNodeId { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = "Hidden"; // "Hidden" | "Available" | "Checked"
}

public enum SessionStatus
{
    SetupInProgress,
    Active,
    Completed,
    Archived
}

// Helper for PocketBase list responses
public class PocketBaseListResponse<T>
{
    [JsonPropertyName("page")]
    public int Page { get; set; }
    [JsonPropertyName("perPage")]
    public int PerPage { get; set; }
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = [];
}
