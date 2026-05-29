using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IT_Gied.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentBenefits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentBenefits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentBenefits", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "StudentBenefits",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "Icon", "IsActive", "Link", "ProviderName", "Title" },
                values: new object[,]
                {
                    { 1, "Developer Tools", new DateTime(2026, 5, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "Access free developer tools, cloud credits, and productivity services with your university email.", "fab fa-github", true, "https://education.github.com/pack", "GitHub", "GitHub Student Developer Pack" },
                    { 2, "Learning Platforms", new DateTime(2026, 5, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "Upgrade your student workspace with Notion's premium features for notes, projects, and collaboration.", "fas fa-book-open", true, "https://www.notion.so/education", "Notion", "Notion Education Plus" },
                    { 3, "Design Tools", new DateTime(2026, 5, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "Create presentations, social assets, and design collateral with premium Canva resources for students.", "fas fa-pencil-ruler", true, "https://www.canva.com/education/", "Canva", "Canva for Education" },
                    { 4, "Developer Tools", new DateTime(2026, 5, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "Use the full JetBrains IDE suite for free while you're enrolled with a valid academic email address.", "fas fa-code", true, "https://www.jetbrains.com/student/", "JetBrains", "JetBrains Student License" },
                    { 5, "AI & Cloud Credits", new DateTime(2026, 5, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "Unlock student-focused learning paths, cloud credits, and AI productivity tools from Microsoft.", "fas fa-cloud", true, "https://learn.microsoft.com/student-hub/", "Microsoft", "Microsoft Learn Student Hub" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentBenefits");
        }
    }
}
