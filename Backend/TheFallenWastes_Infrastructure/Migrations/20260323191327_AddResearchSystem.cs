using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheFallenWastes_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResearchSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Population",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "PopulationCapacity",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "UsedPopulation",
                table: "Settlements");

            migrationBuilder.CreateTable(
                name: "SettlementResearchStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResearchPoints = table.Column<int>(type: "int", nullable: false),
                    MaxConcurrentResearches = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementResearchStates", x => x.Id);
                    table.UniqueConstraint("AK_SettlementResearchStates_SettlementId", x => x.SettlementId);
                });

            migrationBuilder.CreateTable(
                name: "SettlementSalvageInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoredRareTech = table.Column<int>(type: "int", nullable: false),
                    StoredResearchData = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementSalvageInventories", x => x.Id);
                    table.UniqueConstraint("AK_SettlementSalvageInventories_SettlementId", x => x.SettlementId);
                });

            migrationBuilder.CreateTable(
                name: "Researches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Branch = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequiredTechLabLevel = table.Column<int>(type: "int", nullable: false),
                    RareTechCost = table.Column<int>(type: "int", nullable: false),
                    ResearchPointCost = table.Column<int>(type: "int", nullable: false),
                    BaseDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    IsUnlocked = table.Column<bool>(type: "bit", nullable: false),
                    IsResearching = table.Column<bool>(type: "bit", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Researches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Researches_SettlementResearchStates_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "SettlementResearchStates",
                        principalColumn: "SettlementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResearchQueueEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementResearchStateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResearchKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SlotIndex = table.Column<int>(type: "int", nullable: false),
                    QueueOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchQueueEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchQueueEntries_SettlementResearchStates_SettlementResearchStateId",
                        column: x => x.SettlementResearchStateId,
                        principalTable: "SettlementResearchStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalvageItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementSalvageInventoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Rarity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    RequiredTechSalvagerLevel = table.Column<int>(type: "int", nullable: false),
                    BaseSalvageTimeSeconds = table.Column<int>(type: "int", nullable: false),
                    RareTechYield = table.Column<int>(type: "int", nullable: false),
                    ResearchDataYield = table.Column<int>(type: "int", nullable: false),
                    SpecialOutputKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AcquiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalvageItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalvageItems_SettlementSalvageInventories_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "SettlementSalvageInventories",
                        principalColumn: "SettlementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Researches_SettlementId_Key",
                table: "Researches",
                columns: new[] { "SettlementId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchQueueEntries_SettlementResearchStateId_SlotIndex_QueueOrder",
                table: "ResearchQueueEntries",
                columns: new[] { "SettlementResearchStateId", "SlotIndex", "QueueOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SalvageItems_SettlementId_Key",
                table: "SalvageItems",
                columns: new[] { "SettlementId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettlementResearchStates_SettlementId",
                table: "SettlementResearchStates",
                column: "SettlementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettlementSalvageInventories_SettlementId",
                table: "SettlementSalvageInventories",
                column: "SettlementId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Researches");

            migrationBuilder.DropTable(
                name: "ResearchQueueEntries");

            migrationBuilder.DropTable(
                name: "SalvageItems");

            migrationBuilder.DropTable(
                name: "SettlementResearchStates");

            migrationBuilder.DropTable(
                name: "SettlementSalvageInventories");

            migrationBuilder.AddColumn<int>(
                name: "Population",
                table: "Settlements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PopulationCapacity",
                table: "Settlements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsedPopulation",
                table: "Settlements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
