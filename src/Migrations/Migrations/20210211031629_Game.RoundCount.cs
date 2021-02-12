using Microsoft.EntityFrameworkCore.Migrations;

namespace BoardGames.Migrations.Migrations
{
    public partial class GameRoundCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoundCount",
                table: "Games",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoundCount",
                table: "Games");
        }
    }
}
