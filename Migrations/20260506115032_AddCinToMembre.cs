using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tontine.Migrations
{
    /// <inheritdoc />
    public partial class AddCinToMembre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CinRecto",
                table: "Membres",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CinVerso",
                table: "Membres",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CinRecto",
                table: "Membres");

            migrationBuilder.DropColumn(
                name: "CinVerso",
                table: "Membres");
        }
    }
}
