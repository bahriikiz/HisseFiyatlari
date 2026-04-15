using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HisseFiyatlari.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSponsorTableStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SiteType",
                table: "Sponsors");

            migrationBuilder.RenameColumn(
                name: "TargetUrl",
                table: "Sponsors",
                newName: "WebsiteLink");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WebsiteLink",
                table: "Sponsors",
                newName: "TargetUrl");

            migrationBuilder.AddColumn<string>(
                name: "SiteType",
                table: "Sponsors",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
