using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using NetTopologySuite.Geometries;

namespace Bikeapelago.Api.Models;

public class User
{
    [Key]
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

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
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [JsonPropertyName("user")]
    public Guid UserId { get; set; }

    [JsonPropertyName("ap_seed_name")]
    public string? ApSeedName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("ap_server_url")]
    public string? ApServerUrl { get; set; }

    [JsonPropertyName("ap_slot_name")]
    public string? ApSlotName { get; set; }

    [JsonIgnore] // Use DTO for JSON serialization of Point
    public Point? Location { get; set; }

    [JsonPropertyName("radius")]
    public int? Radius { get; set; }

    [JsonPropertyName("received_item_ids")]
    public List<long> ReceivedItemIds { get; set; } = new();

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SessionStatus Status { get; set; } = SessionStatus.SetupInProgress;

    [JsonPropertyName("created")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("updated")]
    public string UpdatedAt { get; set; } = string.Empty;

    [NotMapped]
    [JsonPropertyName("center_lat")]
    public double? CenterLat 
    { 
        get => Location?.Y;
        set => Location = new Point(Location?.X ?? 0, value ?? 0) { SRID = 4326 };
    }

    [NotMapped]
    [JsonPropertyName("center_lon")]
    public double? CenterLon 
    { 
        get => Location?.X;
        set => Location = new Point(value ?? 0, Location?.Y ?? 0) { SRID = 4326 };
    }
}

public class MapNode
{
    [Key]
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [JsonPropertyName("session")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("ap_location_id")]
    public long ApLocationId { get; set; }

    [JsonPropertyName("osm_node_id")]
    public string OsmNodeId { get; set; } = string.Empty;

    [JsonIgnore] // Use DTO for JSON serialization of Point
    public Point? Location { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = "Hidden"; // "Hidden" | "Available" | "Checked"

    [NotMapped]
    [JsonPropertyName("lat")]
    public double? Lat 
    { 
        get => Location?.Y;
        set => Location = new Point(Location?.X ?? 0, value ?? 0) { SRID = 4326 };
    }

    [NotMapped]
    [JsonPropertyName("lon")]
    public double? Lon 
    { 
        get => Location?.X;
        set => Location = new Point(value ?? 0, Location?.Y ?? 0) { SRID = 4326 };
    }
}

public class Route
{
    [Key]
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [JsonPropertyName("user")]
    public Guid UserId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("sport")]
    public string? Sport { get; set; }

    [JsonPropertyName("distance")]
    public double? Distance { get; set; }

    [JsonPropertyName("elevation")]
    public double? Elevation { get; set; }

    [JsonPropertyName("time")]
    public double? Time { get; set; }

    [JsonIgnore]
    public LineString? Path { get; set; }
}

public class Activity
{
    [Key]
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [JsonPropertyName("user")]
    public Guid UserId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("sport")]
    public string? Sport { get; set; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    [JsonPropertyName("tot_distance")]
    public double? TotDistance { get; set; }

    [JsonPropertyName("tot_elevation")]
    public double? TotElevation { get; set; }

    [JsonIgnore]
    public LineString? Path { get; set; }
}

public class ApiLog
{
    [Key]
    public long Id { get; set; }

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(10)]
    public string Method { get; set; } = string.Empty;

    [Required]
    [MaxLength(2048)]
    public string Path { get; set; } = string.Empty;

    public string? QueryString { get; set; }

    [Required]
    public int StatusCode { get; set; }

    [MaxLength(45)] // Enough for IPv6
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? UserId { get; set; }

    public string? ExceptionType { get; set; }

    public string? StackTrace { get; set; }

    public string? RequestBody { get; set; }
}

public enum SessionStatus
{
    SetupInProgress,
    Active,
    Completed,
    Archived
}

// Helper for generic list responses
public class PaginatedListResponse<T>
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
    public virtual List<T> Items { get; set; } = [];
}
