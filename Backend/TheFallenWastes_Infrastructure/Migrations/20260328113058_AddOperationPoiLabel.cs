using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheFallenWastes_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationPoiLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TargetPoiLabel",
                table: "Operations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetPoiLabel",
                table: "Operations");
        }
    }
}
