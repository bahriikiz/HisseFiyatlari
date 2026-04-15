using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HisseFiyatlari.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminPassword",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdminUsername",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminPassword",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "AdminUsername",
                table: "SiteSettings");
        }
    }
}
