using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlackBridge.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveSlackWebhookToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SlackWebhookUrl",
                table: "Projects",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE p
                SET p.SlackWebhookUrl = source.SlackWebhookUrl
                FROM Projects p
                INNER JOIN (
                    SELECT ProjectId, MIN(SlackWebhookUrl) AS SlackWebhookUrl
                    FROM EventDefinitions
                    WHERE SlackWebhookUrl IS NOT NULL AND SlackWebhookUrl <> ''
                    GROUP BY ProjectId
                ) source ON source.ProjectId = p.Id
                WHERE p.SlackWebhookUrl = ''
                """);

            migrationBuilder.DropColumn(
                name: "SlackWebhookUrl",
                table: "EventDefinitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SlackWebhookUrl",
                table: "EventDefinitions",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE e
                SET e.SlackWebhookUrl = p.SlackWebhookUrl
                FROM EventDefinitions e
                INNER JOIN Projects p ON p.Id = e.ProjectId
                """);

            migrationBuilder.DropColumn(
                name: "SlackWebhookUrl",
                table: "Projects");
        }
    }
}
