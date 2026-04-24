using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikeapelago.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportModeAndConnectionMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mode",
                table: "GameSessions");

            migrationBuilder.AddColumn<string>(
                name: "ConnectionMode",
                table: "GameSessions",
                type: "text",
                nullable: false,
                defaultValue: "singleplayer");

            migrationBuilder.AddColumn<string>(
                name: "TransportMode",
                table: "GameSessions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConnectionMode",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "TransportMode",
                table: "GameSessions");

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "GameSessions",
                type: "text",
                nullable: false,
                defaultValue: "bike");
        }
    }
}
