using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikeapelago.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeProgressionModeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProgressionMode",
                table: "GameSessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql("ALTER TABLE \"GameSessions\" ALTER COLUMN \"ProgressionMode\" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProgressionMode",
                table: "GameSessions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
