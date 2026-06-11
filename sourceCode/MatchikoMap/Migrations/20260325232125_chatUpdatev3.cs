using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchikoMap.Migrations
{
    /// <inheritdoc />
    public partial class chatUpdatev3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FileUrl",
                table: "MessageAttachments",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "FileType",
                table: "MessageAttachments",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "FileSize",
                table: "MessageAttachments",
                newName: "Size");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Url",
                table: "MessageAttachments",
                newName: "FileUrl");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "MessageAttachments",
                newName: "FileType");

            migrationBuilder.RenameColumn(
                name: "Size",
                table: "MessageAttachments",
                newName: "FileSize");
        }
    }
}
