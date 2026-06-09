using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrelloApi.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectColumnsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColumnsJson",
                table: "Projects",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColumnsJson",
                table: "Projects");
        }
    }
}
