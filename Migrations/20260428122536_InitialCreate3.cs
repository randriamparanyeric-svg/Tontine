using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tontine.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GroupeId",
                table: "Versements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Statut",
                table: "Versements",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Actif",
                table: "Membres",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateAdhesion",
                table: "Membres",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Membres",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Actif",
                table: "Groupes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CodePartage",
                table: "Groupes",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreation",
                table: "Groupes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Groupes",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantParVersement",
                table: "Groupes",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MotDePasseHash",
                table: "Groupes",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NomAdmin",
                table: "Groupes",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NombreMembresPrevu",
                table: "Groupes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TelephoneAdmin",
                table: "Groupes",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Versements_GroupeId",
                table: "Versements",
                column: "GroupeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Versements_Groupes_GroupeId",
                table: "Versements",
                column: "GroupeId",
                principalTable: "Groupes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Versements_Groupes_GroupeId",
                table: "Versements");

            migrationBuilder.DropIndex(
                name: "IX_Versements_GroupeId",
                table: "Versements");

            migrationBuilder.DropColumn(
                name: "GroupeId",
                table: "Versements");

            migrationBuilder.DropColumn(
                name: "Statut",
                table: "Versements");

            migrationBuilder.DropColumn(
                name: "Actif",
                table: "Membres");

            migrationBuilder.DropColumn(
                name: "DateAdhesion",
                table: "Membres");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Membres");

            migrationBuilder.DropColumn(
                name: "Actif",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "CodePartage",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "DateCreation",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "MontantParVersement",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "MotDePasseHash",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "NomAdmin",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "NombreMembresPrevu",
                table: "Groupes");

            migrationBuilder.DropColumn(
                name: "TelephoneAdmin",
                table: "Groupes");
        }
    }
}
