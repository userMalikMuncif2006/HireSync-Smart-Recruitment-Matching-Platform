using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireSync.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOtpChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailOtpChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Purpose = table.Column<byte>(type: "tinyint", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(44)", maxLength: 44, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailOtpChallenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailOtpChallenges_Email_Purpose_CreatedAtUtc",
                table: "EmailOtpChallenges",
                columns: new[] { "Email", "Purpose", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailOtpChallenges");
        }
    }
}
