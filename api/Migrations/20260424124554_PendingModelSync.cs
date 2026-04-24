using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikeapelago.Api.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SignalAmplifierActive",
                table: "GameSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignalAmplifierActive",
                table: "GameSessions");
        }
    }
}
