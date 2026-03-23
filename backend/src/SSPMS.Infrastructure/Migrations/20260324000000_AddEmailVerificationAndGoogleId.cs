using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSPMS.Infrastructure.Migrations
{
    public partial class AddEmailVerificationAndGoogleId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "Users",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsEmailVerified", table: "Users");
            migrationBuilder.DropColumn(name: "GoogleId", table: "Users");
        }
    }
}
