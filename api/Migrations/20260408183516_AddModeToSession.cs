using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikeapelago.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddModeToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "GameSessions",
                type: "text",
                nullable: false,
                defaultValue: "bike");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mode",
                table: "GameSessions");
        }
    }
}
