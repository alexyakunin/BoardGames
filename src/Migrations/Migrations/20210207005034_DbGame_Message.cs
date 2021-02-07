using Microsoft.EntityFrameworkCore.Migrations;

namespace BoardGames.Migrations.Migrations
{
    // ReSharper disable once InconsistentNaming
    public partial class DbGame_Message : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Message",
                table: "Games");
        }
    }
}
