using Bikeapelago.Api.Models.OsmCache;
using Microsoft.EntityFrameworkCore;

namespace Bikeapelago.Api.Data;

/// <summary>
/// DbContext for the OSM/cache database.
/// Manages only the tables owned by the .NET application (grid cache, jobs).
/// Tables imported by osm2pgsql (planet_osm_nodes, etc.) are unmanaged — accessed
/// via raw SQL within GridCacheService when cross-table queries are needed.
/// </summary>
public class OsmDbContext : DbContext
{
    public OsmDbContext(DbContextOptions<OsmDbContext> options) : base(options) { }

    public DbSet<NodeGridCache> NodeGridCaches { get; set; } = null!;
    public DbSet<GridCacheJob> GridCacheJobs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Composite unique constraint: one cache entry per grid cell + mode
        modelBuilder.Entity<NodeGridCache>()
            .HasIndex(c => new { c.GridX, c.GridY, c.Mode })
            .IsUnique();

        // Plain index — jobs table accumulates history, multiple rows per cell+mode is valid
        modelBuilder.Entity<GridCacheJob>()
            .HasIndex(j => new { j.GridX, j.GridY, j.Mode });

        // Default timestamps via SQL
        modelBuilder.Entity<NodeGridCache>()
            .Property(c => c.CreatedAt)
            .HasDefaultValueSql("NOW()");

        modelBuilder.Entity<NodeGridCache>()
            .Property(c => c.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        modelBuilder.Entity<GridCacheJob>()
            .Property(j => j.CreatedAt)
            .HasDefaultValueSql("NOW()");
    }
}
