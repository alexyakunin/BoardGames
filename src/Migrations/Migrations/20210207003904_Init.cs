using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace BoardGames.Migrations.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "_Operations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AgentId = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CommitTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CommandJson = table.Column<string>(type: "text", nullable: false),
                    ItemsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Operations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "_Sessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IPAddress = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    AuthenticatedIdentity = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    IsSignOutForced = table.Column<bool>(type: "boolean", nullable: false),
                    OptionsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    EngineId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    StateJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    GameEndMessage = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ClaimsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GamePlayers",
                columns: table => new
                {
                    GameId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    EngineId = table.Column<string>(type: "text", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<long>(type: "bigint", nullable: false),
                    DbGameId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePlayers", x => new { x.GameId, x.UserId });
                    table.ForeignKey(
                        name: "FK_GamePlayers_Games_DbGameId",
                        column: x => x.DbGameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserIdentities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Secret = table.Column<string>(type: "text", nullable: false),
                    DbUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserIdentities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserIdentities_Users_DbUserId",
                        column: x => x.DbUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommitTime",
                table: "_Operations",
                column: "CommitTime");

            migrationBuilder.CreateIndex(
                name: "IX_StartTime",
                table: "_Operations",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX__Sessions_CreatedAt_IsSignOutForced",
                table: "_Sessions",
                columns: new[] { "CreatedAt", "IsSignOutForced" });

            migrationBuilder.CreateIndex(
                name: "IX__Sessions_IPAddress_IsSignOutForced",
                table: "_Sessions",
                columns: new[] { "IPAddress", "IsSignOutForced" });

            migrationBuilder.CreateIndex(
                name: "IX__Sessions_LastSeenAt_IsSignOutForced",
                table: "_Sessions",
                columns: new[] { "LastSeenAt", "IsSignOutForced" });

            migrationBuilder.CreateIndex(
                name: "IX__Sessions_UserId_IsSignOutForced",
                table: "_Sessions",
                columns: new[] { "UserId", "IsSignOutForced" });

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_DbGameId",
                table: "GamePlayers",
                column: "DbGameId");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_EngineId_Score",
                table: "GamePlayers",
                columns: new[] { "EngineId", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_Games_Stage_IsPublic_CreatedAt",
                table: "Games",
                columns: new[] { "Stage", "IsPublic", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Games_UserId_CreatedAt_Stage",
                table: "Games",
                columns: new[] { "UserId", "CreatedAt", "Stage" });

            migrationBuilder.CreateIndex(
                name: "IX_Games_UserId_Stage_CreatedAt",
                table: "Games",
                columns: new[] { "UserId", "Stage", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserIdentities_DbUserId",
                table: "UserIdentities",
                column: "DbUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserIdentities_Id",
                table: "UserIdentities",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Name",
                table: "Users",
                column: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "_Operations");

            migrationBuilder.DropTable(
                name: "_Sessions");

            migrationBuilder.DropTable(
                name: "GamePlayers");

            migrationBuilder.DropTable(
                name: "UserIdentities");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
