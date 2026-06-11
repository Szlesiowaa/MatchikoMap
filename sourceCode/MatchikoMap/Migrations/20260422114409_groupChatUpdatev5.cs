using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchikoMap.Migrations
{
    /// <inheritdoc />
    public partial class groupChatUpdatev5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GameId",
                table: "Conversations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserFavouriteGames",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavouriteGames", x => new { x.UserId, x.GameId });
                    table.ForeignKey(
                        name: "FK_UserFavouriteGames_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavouriteGames_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_GameId",
                table: "Conversations",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavouriteGames_GameId",
                table: "UserFavouriteGames",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavouriteGames_UserId",
                table: "UserFavouriteGames",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Games_GameId",
                table: "Conversations",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Games_GameId",
                table: "Conversations");

            migrationBuilder.DropTable(
                name: "UserFavouriteGames");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_GameId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "Conversations");
        }
    }
}
