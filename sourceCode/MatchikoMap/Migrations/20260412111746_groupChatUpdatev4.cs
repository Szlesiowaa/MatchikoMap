using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchikoMap.Migrations
{
    /// <inheritdoc />
    public partial class groupChatUpdatev4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_Name",
                table: "Games");

            migrationBuilder.AddColumn<string>(
                name: "Acronym",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GridPath",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconPath",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PosterPath",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_Name_Trigram",
                table: "Games",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Games_NormalizedName_Trigram",
                table: "Games",
                column: "NormalizedName")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Latitude_Longitude",
                table: "Conversations",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Name_isGroup",
                table: "Conversations",
                columns: new[] { "Name", "isGroup" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_Name_Trigram",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_NormalizedName_Trigram",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_Latitude_Longitude",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_Name_isGroup",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Acronym",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "GridPath",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "IconPath",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "PosterPath",
                table: "Games");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Name",
                table: "Games",
                column: "Name",
                unique: true);
        }
    }
}
