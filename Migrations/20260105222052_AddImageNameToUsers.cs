using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Gied.Migrations
{
    /// <inheritdoc />
    public partial class AddImageNameToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Image_Name",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image_Name",
                table: "AspNetUsers");
        }
    }
}
