using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlackBridge.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSlackCommandRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlackCommandRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerInstanceId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SlackCommand = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EncryptedSlackSigningSecret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DownstreamUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    DownstreamAuthHeaderName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EncryptedDownstreamAuthSecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllowedTeamId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlackCommandRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlackCommandRoutes_CustomerInstances_CustomerInstanceId",
                        column: x => x.CustomerInstanceId,
                        principalTable: "CustomerInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SlackCommandRoutes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlackCommandLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerInstanceId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    SlackCommandRouteId = table.Column<int>(type: "int", nullable: true),
                    Command = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    TeamId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ChannelId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DownstreamStatusCode = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResultMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlackCommandLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlackCommandLogs_CustomerInstances_CustomerInstanceId",
                        column: x => x.CustomerInstanceId,
                        principalTable: "CustomerInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SlackCommandLogs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SlackCommandLogs_SlackCommandRoutes_SlackCommandRouteId",
                        column: x => x.SlackCommandRouteId,
                        principalTable: "SlackCommandRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlackCommandLogs_Command_TeamId",
                table: "SlackCommandLogs",
                columns: new[] { "Command", "TeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlackCommandLogs_CreatedAtUtc",
                table: "SlackCommandLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SlackCommandLogs_CustomerInstanceId",
                table: "SlackCommandLogs",
                column: "CustomerInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlackCommandLogs_ProjectId",
                table: "SlackCommandLogs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SlackCommandLogs_SlackCommandRouteId",
                table: "SlackCommandLogs",
                column: "SlackCommandRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_SlackCommandRoutes_CustomerInstanceId",
                table: "SlackCommandRoutes",
                column: "CustomerInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_SlackCommandRoutes_ProjectId_SlackCommand",
                table: "SlackCommandRoutes",
                columns: new[] { "ProjectId", "SlackCommand" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlackCommandLogs");

            migrationBuilder.DropTable(
                name: "SlackCommandRoutes");
        }
    }
}
