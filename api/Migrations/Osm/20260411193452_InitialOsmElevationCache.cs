using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bikeapelago.Api.Migrations.Osm
{
    /// <inheritdoc />
    public partial class InitialOsmElevationCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure PostGIS raster extension exists
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis_raster CASCADE;");

            // Grid cache jobs table — tracks pending elevation tile downloads
            migrationBuilder.CreateTable(
                name: "grid_cache_jobs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grid_x = table.Column<long>(type: "bigint", nullable: false),
                    grid_y = table.Column<long>(type: "bigint", nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "pending"),
                    data = table.Column<string>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grid_cache_jobs", x => x.id);
                });

            // Partial unique index: only one active (pending/processing) job per grid cell + mode
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS uq_grid_cache_jobs_active
                ON grid_cache_jobs (grid_x, grid_y, mode)
                WHERE status IN ('pending', 'processing');
                """);

            // Regular index for querying
            migrationBuilder.CreateIndex(
                name: "IX_grid_cache_jobs_grid_x_grid_y_mode",
                table: "grid_cache_jobs",
                columns: new[] { "grid_x", "grid_y", "mode" });

            // SRTM elevation raster table — stores SRTM tiles as PostGIS rasters
            migrationBuilder.CreateTable(
                name: "srtm_elevation",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rast = table.Column<byte[]>(type: "raster", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_srtm_elevation", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "srtm_elevation");
            migrationBuilder.Sql("DROP INDEX IF EXISTS uq_grid_cache_jobs_active;");
            migrationBuilder.DropTable(name: "grid_cache_jobs");
        }
    }
}
