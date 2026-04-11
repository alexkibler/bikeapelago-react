using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bikeapelago.Api.Models.OsmCache;

[Table("grid_cache_jobs")]
public class GridCacheJob
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("grid_x")]
    public long GridX { get; set; }

    [Column("grid_y")]
    public long GridY { get; set; }

    [Column("mode")]
    [MaxLength(32)]
    public string Mode { get; set; } = "bike";

    [Column("status")]
    [MaxLength(32)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Optional JSON payload (e.g. tile code + session ID for elevation jobs).
    /// </summary>
    [Column("data", TypeName = "jsonb")]
    public string? Data { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
}
