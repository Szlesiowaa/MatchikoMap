using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchikoMap.Migrations
{
    /// <inheritdoc />
    public partial class chatUpdatev4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReadAt",
                table: "ConversationMembers");

            migrationBuilder.AddColumn<int>(
                name: "LastReadMessageId",
                table: "ConversationMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReadMessageId",
                table: "ConversationMembers");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReadAt",
                table: "ConversationMembers",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
