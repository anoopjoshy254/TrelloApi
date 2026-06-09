using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrelloApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskAssignmentCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "TaskAssignments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "TaskAssignments",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "TaskAssignments");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "TaskAssignments");
        }
    }
}
