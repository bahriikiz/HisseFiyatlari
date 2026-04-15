using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HisseFiyatlari.Migrations
{
    /// <inheritdoc />
    public partial class AddNewAdFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LeftAdCode",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RightAdCode",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopAdCode",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeftAdCode",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "RightAdCode",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "TopAdCode",
                table: "SiteSettings");
        }
    }
}
