using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using NetTopologySuite.Geometries;

namespace Bikeapelago.Api.Models;

public class User : IdentityUser<Guid>
{
    // Id is inherited from IdentityUser<Guid>

    [JsonPropertyName("username")]
    public override string? UserName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("weight")]
    public double? Weight { get; set; } = 75.0;

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("email")]
    public override string? Email { get; set; }

    [JsonIgnore]
    public string? Password { get; set; } 
    // Note: PasswordHash is inherited, but we might keep this for local compatibility if needed 
    // although UserManager will handle it.
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

    [JsonPropertyName("connection_mode")]
    public string ConnectionMode { get; set; } = "singleplayer"; // "archipelago" | "singleplayer"

    [JsonPropertyName("transport_mode")]
    public string TransportMode { get; set; } = "bike"; // "bike" | "walk"

    [JsonPropertyName("progression_mode")]
    public string? ProgressionMode { get; set; } // "quadrant" | "radius" | "free"

    [JsonPropertyName("north_pass_received")]
    public bool NorthPassReceived { get; set; }

    [JsonPropertyName("east_pass_received")]
    public bool EastPassReceived { get; set; }

    [JsonPropertyName("south_pass_received")]
    public bool SouthPassReceived { get; set; }

    [JsonPropertyName("west_pass_received")]
    public bool WestPassReceived { get; set; }

    [JsonPropertyName("detours_used")]
    public int DetoursUsed { get; set; } = 0;

    [JsonPropertyName("drones_used")]
    public int DronesUsed { get; set; } = 0;

    [JsonPropertyName("signal_amplifiers_used")]
    public int SignalAmplifiersUsed { get; set; } = 0;

    [JsonPropertyName("radius_step")]
    public int RadiusStep { get; set; } = 0; // 0=25%, 1=50%, 2=75%, 3=100%

    [JsonPropertyName("signal_amplifier_active")]
    public bool SignalAmplifierActive { get; set; }

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

    [JsonPropertyName("ap_arrival_location_id")]
    public long ApArrivalLocationId { get; set; }

    [JsonPropertyName("ap_precision_location_id")]
    public long ApPrecisionLocationId { get; set; }

    [JsonPropertyName("osm_node_id")]
    public string OsmNodeId { get; set; } = string.Empty;

    [JsonIgnore] // Use DTO for JSON serialization of Point
    public Point? Location { get; set; }

    [JsonPropertyName("region_tag")]
    public string RegionTag { get; set; } = "Hub"; // "Hub", "North", "East", "South", "West"

    [JsonPropertyName("state")]
    public string State { get; set; } = "Hidden"; // "Hidden" | "Available" | "Checked"

    [JsonPropertyName("is_arrival_checked")]
    public bool IsArrivalChecked { get; set; }

    [JsonPropertyName("is_precision_checked")]
    public bool IsPrecisionChecked { get; set; }

    [JsonPropertyName("has_been_relocated")]
    public bool HasBeenRelocated { get; set; }

    [JsonPropertyName("arrival_reward_item_id")]
    public long? ArrivalRewardItemId { get; set; }

    [JsonPropertyName("arrival_reward_item_name")]
    public string? ArrivalRewardItemName { get; set; }

    [JsonPropertyName("precision_reward_item_id")]
    public long? PrecisionRewardItemId { get; set; }

    [JsonPropertyName("precision_reward_item_name")]
    public string? PrecisionRewardItemName { get; set; }

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
