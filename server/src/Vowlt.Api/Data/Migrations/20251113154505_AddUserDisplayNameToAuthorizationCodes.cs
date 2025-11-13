using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vowlt.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDisplayNameToAuthorizationCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserDisplayName",
                table: "AuthorizationCodes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserDisplayName",
                table: "AuthorizationCodes");
        }
    }
}
