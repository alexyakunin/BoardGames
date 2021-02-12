using Microsoft.EntityFrameworkCore.Migrations;

namespace BoardGames.Migrations.Migrations
{
    public partial class GameRoundIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoundIndex",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoundIndex",
                table: "Games");
        }
    }
}
