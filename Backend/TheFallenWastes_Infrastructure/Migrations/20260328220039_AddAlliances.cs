using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheFallenWastes_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlliances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AllianceId",
                table: "Players",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Alliances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MinPoints = table.Column<int>(type: "int", nullable: false),
                    FounderPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoundedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alliances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AllianceApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllianceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsInvitation = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllianceApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllianceApplications_Alliances_AllianceId",
                        column: x => x.AllianceId,
                        principalTable: "Alliances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AllianceForumTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllianceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AuthorPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllianceForumTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllianceForumTopics_Alliances_AllianceId",
                        column: x => x.AllianceId,
                        principalTable: "Alliances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AllianceMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllianceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rank = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllianceMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllianceMembers_Alliances_AllianceId",
                        column: x => x.AllianceId,
                        principalTable: "Alliances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AllianceForumPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EditedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllianceForumPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllianceForumPosts_AllianceForumTopics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "AllianceForumTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllianceApplications_AllianceId",
                table: "AllianceApplications",
                column: "AllianceId");

            migrationBuilder.CreateIndex(
                name: "IX_AllianceForumPosts_TopicId",
                table: "AllianceForumPosts",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_AllianceForumTopics_AllianceId",
                table: "AllianceForumTopics",
                column: "AllianceId");

            migrationBuilder.CreateIndex(
                name: "IX_AllianceMembers_AllianceId_PlayerId",
                table: "AllianceMembers",
                columns: new[] { "AllianceId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alliances_Name",
                table: "Alliances",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alliances_Tag",
                table: "Alliances",
                column: "Tag",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllianceApplications");

            migrationBuilder.DropTable(
                name: "AllianceForumPosts");

            migrationBuilder.DropTable(
                name: "AllianceMembers");

            migrationBuilder.DropTable(
                name: "AllianceForumTopics");

            migrationBuilder.DropTable(
                name: "Alliances");

            migrationBuilder.DropColumn(
                name: "AllianceId",
                table: "Players");
        }
    }
}
