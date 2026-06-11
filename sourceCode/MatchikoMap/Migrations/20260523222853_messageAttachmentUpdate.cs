using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchikoMap.Migrations
{
    /// <inheritdoc />
    public partial class messageAttachmentUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Url",
                table: "MessageAttachments",
                newName: "BlobName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BlobName",
                table: "MessageAttachments",
                newName: "Url");
        }
    }
}
