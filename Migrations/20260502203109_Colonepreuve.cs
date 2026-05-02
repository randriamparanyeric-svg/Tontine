using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tontine.Migrations
{
    /// <inheritdoc />
    public partial class Colonepreuve : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModePaiement",
                table: "Versements",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreuveImage",
                table: "Versements",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModePaiement",
                table: "Versements");

            migrationBuilder.DropColumn(
                name: "PreuveImage",
                table: "Versements");
        }
    }
}
