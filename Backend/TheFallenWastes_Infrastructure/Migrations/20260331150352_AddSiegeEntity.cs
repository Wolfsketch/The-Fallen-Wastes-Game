using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheFallenWastes_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSiegeEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sieges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttackerPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefenderPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConvoyOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GarrisonUnitsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sieges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sieges_AttackerPlayerId",
                table: "Sieges",
                column: "AttackerPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sieges_DefenderPlayerId",
                table: "Sieges",
                column: "DefenderPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sieges_SettlementId_Status",
                table: "Sieges",
                columns: new[] { "SettlementId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sieges");
        }
    }
}
