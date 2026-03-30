using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AltinZamani.Migrations
{
    /// <inheritdoc />
    public partial class AddSEOFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaKeywords",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetaDescription",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "MetaKeywords",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "SiteSettings");
        }
    }
}
