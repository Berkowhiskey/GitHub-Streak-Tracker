using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreakTracker.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestoneToNotificationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MilestoneDay",
                table: "notification_logs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_UserId_MilestoneDay",
                table: "notification_logs",
                columns: new[] { "UserId", "MilestoneDay" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_logs_UserId_MilestoneDay",
                table: "notification_logs");

            migrationBuilder.DropColumn(
                name: "MilestoneDay",
                table: "notification_logs");
        }
    }
}
