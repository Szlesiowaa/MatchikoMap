using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MatchikoMap.Migrations
{
    /// <inheritdoc />
    public partial class matchmakingUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SearchRadiusKm",
                table: "UserSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "RadiusKm",
                table: "Users",
                type: "double precision",
                nullable: false,
                defaultValue: 30.0);

            migrationBuilder.CreateTable(
                name: "MatchmakingEntries",
                columns: table => new
                {
                    MatchId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    CreatorUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatorStatus = table.Column<string>(type: "text", nullable: false),
                    JoinerUserId = table.Column<int>(type: "integer", nullable: true),
                    JoinerStatus = table.Column<string>(type: "text", nullable: true),
                    ExpiringAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchmakingEntries", x => x.MatchId);
                    table.ForeignKey(
                        name: "FK_MatchmakingEntries_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchmakingEntries_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchmakingEntries_Users_JoinerUserId",
                        column: x => x.JoinerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchmakingRejects",
                columns: table => new
                {
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchmakingRejects", x => new { x.MatchId, x.UserId });
                    table.ForeignKey(
                        name: "FK_MatchmakingRejects_MatchmakingEntries_MatchId",
                        column: x => x.MatchId,
                        principalTable: "MatchmakingEntries",
                        principalColumn: "MatchId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchmakingRejects_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchmakingEntries_CreatorUserId_GameId",
                table: "MatchmakingEntries",
                columns: new[] { "CreatorUserId", "GameId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchmakingEntries_GameId_CreatorStatus_ExpiringAt",
                table: "MatchmakingEntries",
                columns: new[] { "GameId", "CreatorStatus", "ExpiringAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchmakingEntries_JoinerUserId",
                table: "MatchmakingEntries",
                column: "JoinerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchmakingRejects_MatchId_UserId",
                table: "MatchmakingRejects",
                columns: new[] { "MatchId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchmakingRejects_UserId",
                table: "MatchmakingRejects",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchmakingRejects");

            migrationBuilder.DropTable(
                name: "MatchmakingEntries");

            migrationBuilder.DropColumn(
                name: "SearchRadiusKm",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "RadiusKm",
                table: "Users");
        }
    }
}
