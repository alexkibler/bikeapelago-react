using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bikeapelago.Api.Models.OsmCache;

[Table("node_grid_cache")]
public class NodeGridCache
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

    [Column("node_ids")]
    public long[] NodeIds { get; set; } = [];

    [Column("node_count")]
    public long NodeCount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
