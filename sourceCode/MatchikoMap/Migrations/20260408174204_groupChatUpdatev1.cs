using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchikoMap.Migrations
{
    /// <inheritdoc />
    public partial class groupChatUpdatev1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConversationId",
                table: "FriendRelations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Conversations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Conversations",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FriendRelations_ConversationId",
                table: "FriendRelations",
                column: "ConversationId");

            migrationBuilder.AddForeignKey(
                name: "FK_FriendRelations_Conversations_ConversationId",
                table: "FriendRelations",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FriendRelations_Conversations_ConversationId",
                table: "FriendRelations");

            migrationBuilder.DropIndex(
                name: "IX_FriendRelations_ConversationId",
                table: "FriendRelations");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "FriendRelations");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Conversations");
        }
    }
}
