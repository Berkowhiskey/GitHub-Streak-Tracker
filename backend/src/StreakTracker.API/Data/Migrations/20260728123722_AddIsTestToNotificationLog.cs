using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreakTracker.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsTestToNotificationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTest",
                table: "notification_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTest",
                table: "notification_logs");
        }
    }
}
