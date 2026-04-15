using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HisseFiyatlari.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerAdField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerAdCode",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerAdCode",
                table: "SiteSettings");
        }
    }
}
