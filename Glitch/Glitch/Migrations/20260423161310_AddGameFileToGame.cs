using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glitch.Migrations
{
    /// <inheritdoc />
    public partial class AddGameFileToGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GameFile",
                table: "Games",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameFile",
                table: "Games");
        }
    }
}
