using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Gied.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseCreditHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCourseProgresses_AspNetUsers_UserId",
                table: "UserCourseProgresses");

            migrationBuilder.AddColumn<int>(
                name: "CreditHours",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DetailsUrl",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCourseProgresses_AspNetUsers_UserId",
                table: "UserCourseProgresses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCourseProgresses_AspNetUsers_UserId",
                table: "UserCourseProgresses");

            migrationBuilder.DropColumn(
                name: "CreditHours",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DetailsUrl",
                table: "Courses");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCourseProgresses_AspNetUsers_UserId",
                table: "UserCourseProgresses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
