using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheFallenWastes_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiStateNpcLoot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInitialized",
                table: "PoiStates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LootItemsJson",
                table: "PoiStates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NpcUnitsJson",
                table: "PoiStates",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInitialized",
                table: "PoiStates");

            migrationBuilder.DropColumn(
                name: "LootItemsJson",
                table: "PoiStates");

            migrationBuilder.DropColumn(
                name: "NpcUnitsJson",
                table: "PoiStates");
        }
    }
}
