using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_Gied.Migrations
{
    public partial class InitPrerequisitesAndProgressTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) CoursePrerequisites
            migrationBuilder.CreateTable(
                name: "CoursePrerequisites",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    CourseId = table.Column<int>(nullable: false),
                    PrerequisiteCourseId = table.Column<int>(nullable: false),

                    RelationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoursePrerequisites", x => x.Id);

                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_PrerequisiteCourseId",
                        column: x => x.PrerequisiteCourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoursePrerequisites_CourseId_PrerequisiteCourseId_RelationType",
                table: "CoursePrerequisites",
                columns: new[] { "CourseId", "PrerequisiteCourseId", "RelationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoursePrerequisites_PrerequisiteCourseId",
                table: "CoursePrerequisites",
                column: "PrerequisiteCourseId");


            // 2) UserCourseProgresses
            migrationBuilder.CreateTable(
                name: "UserCourseProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CourseId = table.Column<int>(nullable: false),

                    IsCompleted = table.Column<bool>(nullable: false),
                    CompletedAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCourseProgresses", x => x.Id);

                    table.ForeignKey(
                        name: "FK_UserCourseProgresses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade); // إذا انحذف المستخدم احذف تقدمه

                    table.ForeignKey(
                        name: "FK_UserCourseProgresses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict); // لا تحذف تقدم لو انحذفت مادة (أمان)
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCourseProgresses_UserId_CourseId",
                table: "UserCourseProgresses",
                columns: new[] { "UserId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCourseProgresses_CourseId",
                table: "UserCourseProgresses",
                column: "CourseId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserCourseProgresses");
            migrationBuilder.DropTable(name: "CoursePrerequisites");
        }
    }
}
