using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheFallenWastes_Infrastructure.Migrations
{
    public partial class AddMissingAdvisorColumnsFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "QuartermasterActive",
                table: "Players",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "QuartermasterExpiresUtc",
                table: "Players",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ScoutMasterActive",
                table: "Players",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScoutMasterExpiresUtc",
                table: "Players",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TechPriestActive",
                table: "Players",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TechPriestExpiresUtc",
                table: "Players",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuartermasterActive",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "QuartermasterExpiresUtc",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ScoutMasterActive",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ScoutMasterExpiresUtc",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TechPriestActive",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TechPriestExpiresUtc",
                table: "Players");
        }
    }
}