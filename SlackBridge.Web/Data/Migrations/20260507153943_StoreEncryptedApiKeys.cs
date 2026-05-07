using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlackBridge.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class StoreEncryptedApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedKey",
                table: "ApiKeys",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedKey",
                table: "ApiKeys");
        }
    }
}
