using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KontrolaNawykow.Migrations
{
    /// <inheritdoc />
    public partial class AddNutritionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomCarbsGrams",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomFatGrams",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomProteinGrams",
                table: "Users",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomCarbsGrams",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CustomFatGrams",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CustomProteinGrams",
                table: "Users");
        }
    }
}
