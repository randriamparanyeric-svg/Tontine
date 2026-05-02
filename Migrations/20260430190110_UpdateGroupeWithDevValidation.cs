using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tontine.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGroupeWithDevValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Actif",
                table: "Groupes",
                newName: "EstValideParDev");

            migrationBuilder.AddColumn<string>(
                name: "AdminEmail",
                table: "Groupes",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "Groupes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Groupes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminEmail",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "Groupes");

            migrationBuilder.RenameColumn(
                name: "EstValideParDev",
                table: "Groupes",
                newName: "Actif");
        }
    }
}
