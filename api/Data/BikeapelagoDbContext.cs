using Bikeapelago.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace Bikeapelago.Api.Data;

public class BikeapelagoDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public BikeapelagoDbContext(DbContextOptions<BikeapelagoDbContext> options) : base(options)
    {
    }

    public DbSet<GameSession> GameSessions { get; set; } = null!;
    public DbSet<MapNode> MapNodes { get; set; } = null!;

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


    }
}
