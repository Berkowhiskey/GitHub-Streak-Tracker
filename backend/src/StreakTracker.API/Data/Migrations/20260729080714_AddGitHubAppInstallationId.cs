using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreakTracker.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGitHubAppInstallationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GitHubAppInstallationId",
                table: "users",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubAppInstallationId",
                table: "users");
        }
    }
}
