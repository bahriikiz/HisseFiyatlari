using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AltinZamani.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSiteSettingsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DataRetentionsDays",
                table: "SiteSettings",
                newName: "DataRetentionDays");

            migrationBuilder.AddColumn<int>(
                name: "CleanupIntervalInHours",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CleanupIntervalInHours",
                table: "SiteSettings");

            migrationBuilder.RenameColumn(
                name: "DataRetentionDays",
                table: "SiteSettings",
                newName: "DataRetentionsDays");
        }
    }
}
