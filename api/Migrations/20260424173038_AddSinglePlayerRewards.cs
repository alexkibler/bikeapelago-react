using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikeapelago.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSinglePlayerRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ArrivalRewardItemId",
                table: "MapNodes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArrivalRewardItemName",
                table: "MapNodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PrecisionRewardItemId",
                table: "MapNodes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrecisionRewardItemName",
                table: "MapNodes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArrivalRewardItemId",
                table: "MapNodes");

            migrationBuilder.DropColumn(
                name: "ArrivalRewardItemName",
                table: "MapNodes");

            migrationBuilder.DropColumn(
                name: "PrecisionRewardItemId",
                table: "MapNodes");

            migrationBuilder.DropColumn(
                name: "PrecisionRewardItemName",
                table: "MapNodes");
        }
    }
}
