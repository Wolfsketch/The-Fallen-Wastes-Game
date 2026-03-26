using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheFallenWastes_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingUpgradeQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BuildingUpgradeQueueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildingType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetLevel = table.Column<int>(type: "int", nullable: false),
                    CostWater = table.Column<int>(type: "int", nullable: false),
                    CostFood = table.Column<int>(type: "int", nullable: false),
                    CostScrap = table.Column<int>(type: "int", nullable: false),
                    CostFuel = table.Column<int>(type: "int", nullable: false),
                    CostEnergy = table.Column<int>(type: "int", nullable: false),
                    CostRareTech = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsStarted = table.Column<bool>(type: "bit", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActiveBuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingUpgradeQueueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuildingUpgradeQueueItems_Settlements_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "Settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingUpgradeQueueItems_ActiveBuildingId",
                table: "BuildingUpgradeQueueItems",
                column: "ActiveBuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingUpgradeQueueItems_SettlementId_IsStarted_TargetLevel",
                table: "BuildingUpgradeQueueItems",
                columns: new[] { "SettlementId", "IsStarted", "TargetLevel" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuildingUpgradeQueueItems");
        }
    }
}
