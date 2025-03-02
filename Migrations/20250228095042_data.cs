using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace KontrolaNawykow.Migrations
{
    public partial class data : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "MealPlans",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 1, 1));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "MealPlans");
        }
    }
}
