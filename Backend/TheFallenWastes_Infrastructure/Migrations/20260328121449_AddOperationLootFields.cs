using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheFallenWastes_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationLootFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LootIntervalSeconds",
                table: "Operations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LootItemsCollected",
                table: "Operations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LootIntervalSeconds",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "LootItemsCollected",
                table: "Operations");
        }
    }
}
