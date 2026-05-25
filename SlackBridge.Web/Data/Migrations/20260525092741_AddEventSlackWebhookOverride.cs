using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlackBridge.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSlackWebhookOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomSlackWebhookUrl",
                table: "EventDefinitions",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseCustomSlackWebhook",
                table: "EventDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomSlackWebhookUrl",
                table: "EventDefinitions");

            migrationBuilder.DropColumn(
                name: "UseCustomSlackWebhook",
                table: "EventDefinitions");
        }
    }
}
