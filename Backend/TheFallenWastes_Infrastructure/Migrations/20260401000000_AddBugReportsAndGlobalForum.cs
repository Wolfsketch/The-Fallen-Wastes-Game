using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheFallenWastes_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBugReportsAndGlobalForum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BugReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Area = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    StepsToReproduce = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SettlementName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Browser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BugReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BugReports_PlayerId",
                table: "BugReports",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_BugReports_PlayerId_CreatedAtUtc",
                table: "BugReports",
                columns: new[] { "PlayerId", "CreatedAtUtc" });

            migrationBuilder.CreateTable(
                name: "ForumTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    AuthorPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUsername = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    IsOfficial = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastPostAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastPostPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastPostUsername = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumTopics", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForumTopics_CategoryKey",
                table: "ForumTopics",
                column: "CategoryKey");

            migrationBuilder.CreateIndex(
                name: "IX_ForumTopics_LastPostAtUtc",
                table: "ForumTopics",
                column: "LastPostAtUtc");

            migrationBuilder.CreateTable(
                name: "ForumPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUsername = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForumPosts_ForumTopics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "ForumTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_TopicId",
                table: "ForumPosts",
                column: "TopicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ForumPosts");
            migrationBuilder.DropTable(name: "ForumTopics");
            migrationBuilder.DropTable(name: "BugReports");
        }
    }
}
