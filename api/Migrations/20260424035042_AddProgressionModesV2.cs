using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikeapelago.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressionModesV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApLocationId",
                table: "MapNodes",
                newName: "ApPrecisionLocationId");

            migrationBuilder.AddColumn<long>(
                name: "ApArrivalLocationId",
                table: "MapNodes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "HasBeenRelocated",
                table: "MapNodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsArrivalChecked",
                table: "MapNodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrecisionChecked",
                table: "MapNodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RegionTag",
                table: "MapNodes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EastPassReceived",
                table: "GameSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NorthPassReceived",
                table: "GameSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProgressionMode",
                table: "GameSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RadiusStep",
                table: "GameSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SouthPassReceived",
                table: "GameSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WestPassReceived",
                table: "GameSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApArrivalLocationId",
                table: "MapNodes");

            migrationBuilder.DropColumn(
                name: "HasBeenRelocated",
                table: "MapNodes");

            migrationBuilder.DropColumn(
                name: "IsArrivalChecked",
                table: "MapNodes");

            migrationBuilder.DropColumn(
                name: "IsPrecisionChecked",
                table: "MapNodes");

            migrationBuilder.DropColumn(
                name: "RegionTag",
                table: "MapNodes");

            migrationBuilder.DropColumn(
                name: "EastPassReceived",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "NorthPassReceived",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "ProgressionMode",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "RadiusStep",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "SouthPassReceived",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "WestPassReceived",
                table: "GameSessions");

            migrationBuilder.RenameColumn(
                name: "ApPrecisionLocationId",
                table: "MapNodes",
                newName: "ApLocationId");
        }
    }
}
