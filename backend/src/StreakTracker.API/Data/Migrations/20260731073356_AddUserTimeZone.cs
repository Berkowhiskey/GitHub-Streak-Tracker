using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreakTracker.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTimeZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PreferredNotificationHourUtc",
                table: "users",
                newName: "PreferredNotificationHour");

            migrationBuilder.RenameIndex(
                name: "IX_users_IsActive_PreferredNotificationHourUtc",
                table: "users",
                newName: "IX_users_IsActive_PreferredNotificationHour");

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "UTC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "PreferredNotificationHour",
                table: "users",
                newName: "PreferredNotificationHourUtc");

            migrationBuilder.RenameIndex(
                name: "IX_users_IsActive_PreferredNotificationHour",
                table: "users",
                newName: "IX_users_IsActive_PreferredNotificationHourUtc");
        }
    }
}
