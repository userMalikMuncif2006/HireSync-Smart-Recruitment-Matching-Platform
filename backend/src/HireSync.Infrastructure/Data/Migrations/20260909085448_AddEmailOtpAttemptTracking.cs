using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireSync.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOtpAttemptTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedAttempts",
                table: "EmailOtpChallenges",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedAttempts",
                table: "EmailOtpChallenges");
        }
    }
}
