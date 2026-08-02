using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreakTracker.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BadgeSettingsJson",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BadgeSettingsSignature",
                table: "users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BadgeSettingsJson",
                table: "users");

            migrationBuilder.DropColumn(
                name: "BadgeSettingsSignature",
                table: "users");
        }
    }
}
