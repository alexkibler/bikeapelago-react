using Bikeapelago.Api.Models;
using Microsoft.EntityFrameworkCore;
using Route = Bikeapelago.Api.Models.Route;

namespace Bikeapelago.Api.Data;

public class BikeapelagoDbContext : DbContext
{
    public BikeapelagoDbContext(DbContextOptions<BikeapelagoDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<GameSession> GameSessions { get; set; } = null!;
    public DbSet<MapNode> MapNodes { get; set; } = null!;
    public DbSet<Route> Routes { get; set; } = null!;
    public DbSet<Activity> Activities { get; set; } = null!;
    public DbSet<ApiLog> ApiLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GameSession>()
            .Property(g => g.Status)
            .HasConversion<string>();

        // Map Spatial Data Types
        modelBuilder.Entity<GameSession>()
            .Property(g => g.Location)
            .HasColumnType("geometry (point)");

        modelBuilder.Entity<MapNode>()
            .Property(m => m.Location)
            .HasColumnType("geometry (point)");

        modelBuilder.Entity<Route>()
            .Property(r => r.Path)
            .HasColumnType("geometry (linestring)");

        modelBuilder.Entity<Activity>()
            .Property(a => a.Path)
            .HasColumnType("geometry (linestring)");

        // Add foreign key relationships explicitly if needed
        modelBuilder.Entity<GameSession>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MapNode>()
            .HasOne<GameSession>()
            .WithMany()
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Route>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Activity>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
