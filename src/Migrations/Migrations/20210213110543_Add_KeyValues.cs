using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BoardGames.Migrations.Migrations
{
    public partial class Add_KeyValues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserIdentities_Users_DbUserId",
                table: "UserIdentities");

            migrationBuilder.DropIndex(
                name: "IX_UserIdentities_DbUserId",
                table: "UserIdentities");

            migrationBuilder.DropColumn(
                name: "DbUserId",
                table: "UserIdentities");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "_Sessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "_KeyValues",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__KeyValues", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserIdentities_UserId",
                table: "UserIdentities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX__KeyValues_ExpiresAt",
                table: "_KeyValues",
                column: "ExpiresAt");

            migrationBuilder.AddForeignKey(
                name: "FK_UserIdentities_Users_UserId",
                table: "UserIdentities",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserIdentities_Users_UserId",
                table: "UserIdentities");

            migrationBuilder.DropTable(
                name: "_KeyValues");

            migrationBuilder.DropIndex(
                name: "IX_UserIdentities_UserId",
                table: "UserIdentities");

            migrationBuilder.AddColumn<long>(
                name: "DbUserId",
                table: "UserIdentities",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "_Sessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.CreateIndex(
                name: "IX_UserIdentities_DbUserId",
                table: "UserIdentities",
                column: "DbUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserIdentities_Users_DbUserId",
                table: "UserIdentities",
                column: "DbUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
