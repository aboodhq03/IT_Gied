using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Gied.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackNodeDetailsUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DetailsUrl",
                table: "TrackNodes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DetailsUrl",
                table: "TrackNodes");
        }
    }
}
