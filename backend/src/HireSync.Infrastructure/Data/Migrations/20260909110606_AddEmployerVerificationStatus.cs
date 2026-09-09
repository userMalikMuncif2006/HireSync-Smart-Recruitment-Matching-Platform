using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireSync.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployerVerificationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "EmployerVerificationStatus",
                table: "AspNetUsers",
                type: "tinyint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployerVerificationStatus",
                table: "AspNetUsers");
        }
    }
}
