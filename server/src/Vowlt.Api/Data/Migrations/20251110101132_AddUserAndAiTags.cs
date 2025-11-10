using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vowlt.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndAiTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "GeneratedTags",
                table: "Bookmarks",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "Bookmarks",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_GeneratedTags",
                table: "Bookmarks",
                column: "GeneratedTags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_Tags",
                table: "Bookmarks",
                column: "Tags")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_GeneratedTags",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_Tags",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "GeneratedTags",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Bookmarks");
        }
    }
}
