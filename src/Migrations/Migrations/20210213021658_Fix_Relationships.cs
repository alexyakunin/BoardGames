using Microsoft.EntityFrameworkCore.Migrations;

namespace BoardGames.Migrations.Migrations
{
    public partial class Fix_Relationships : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GamePlayers_Games_DbGameId",
                table: "GamePlayers");

            migrationBuilder.DropIndex(
                name: "IX_GamePlayers_DbGameId",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "DbGameId",
                table: "GamePlayers");

            migrationBuilder.AddForeignKey(
                name: "FK_GamePlayers_Games_GameId",
                table: "GamePlayers",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GamePlayers_Games_GameId",
                table: "GamePlayers");

            migrationBuilder.AddColumn<string>(
                name: "DbGameId",
                table: "GamePlayers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_DbGameId",
                table: "GamePlayers",
                column: "DbGameId");

            migrationBuilder.AddForeignKey(
                name: "FK_GamePlayers_Games_DbGameId",
                table: "GamePlayers",
                column: "DbGameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
