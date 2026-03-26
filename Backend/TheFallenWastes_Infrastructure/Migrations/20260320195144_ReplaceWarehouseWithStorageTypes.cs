using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheFallenWastes_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceWarehouseWithStorageTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Buildings WHERE Type = 'Warehouse'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot restore old Warehouse data
        }
    }
}
