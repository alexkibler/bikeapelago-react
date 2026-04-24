using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikeapelago.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMacguffinWinCondition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MacguffinsCollected",
                table: "GameSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MacguffinsRequired",
                table: "GameSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MacguffinsCollected",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "MacguffinsRequired",
                table: "GameSessions");
        }
    }
}
