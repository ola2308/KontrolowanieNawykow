using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KontrolaNawykow.Migrations
{
    /// <inheritdoc />
    public partial class Plec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Plec",
                table: "Users",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Plec",
                table: "Users");
        }
    }
}
