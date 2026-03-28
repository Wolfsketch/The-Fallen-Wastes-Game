using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheFallenWastes_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAllianceMemberPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanBroadcast",
                table: "AllianceMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanInvite",
                table: "AllianceMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageRecruitment",
                table: "AllianceMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageReservations",
                table: "AllianceMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsForumModerator",
                table: "AllianceMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanBroadcast",
                table: "AllianceMembers");

            migrationBuilder.DropColumn(
                name: "CanInvite",
                table: "AllianceMembers");

            migrationBuilder.DropColumn(
                name: "CanManageRecruitment",
                table: "AllianceMembers");

            migrationBuilder.DropColumn(
                name: "CanManageReservations",
                table: "AllianceMembers");

            migrationBuilder.DropColumn(
                name: "IsForumModerator",
                table: "AllianceMembers");
        }
    }
}
