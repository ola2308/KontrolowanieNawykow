using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KontrolaNawykow.Migrations
{
    /// <inheritdoc />
    public partial class AddBmiAndCaloriesDeficitFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CustomBmi",
                table: "Users",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomCaloriesDeficit",
                table: "Users",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomBmi",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CustomCaloriesDeficit",
                table: "Users");
        }
    }
}
