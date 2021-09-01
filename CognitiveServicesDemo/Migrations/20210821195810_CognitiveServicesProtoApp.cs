using Microsoft.EntityFrameworkCore.Migrations;

namespace CognitiveServicesDemo.Migrations
{
    public partial class CognitiveServicesDemo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaId",
                columns: table => new
                {
                    Sql_id = table.Column<int>(type: "int", nullable: false),
                    Cosmos_db = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Storage_table = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Storage_blob = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaId", x => x.Sql_id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaId");
        }
    }
}
