using Microsoft.EntityFrameworkCore.Migrations;

namespace CognitiveServicesDemo.Migrations
{
    public partial class ComputerVisionWebAppMigrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "UserMedia",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "UserMedia");
        }
    }
}
