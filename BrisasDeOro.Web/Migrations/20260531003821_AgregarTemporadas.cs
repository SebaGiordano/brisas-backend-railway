using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrisasDeOro.Web.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTemporadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tarifas_AlojamientoId_CantidadPersonas",
                table: "Tarifas");

            migrationBuilder.AddColumn<int>(
                name: "TemporadaId",
                table: "Tarifas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Temporadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Temporadas", x => x.Id);
                });

            // ── Data migration ─────────────────────────────────────────────────
            // Insertar las dos temporadas base
            migrationBuilder.Sql(@"
                INSERT INTO Temporadas (Nombre, FechaInicio, FechaFin, Activo)
                VALUES
                    ('Temporada Alta', '2025-12-24', '2026-04-19', 1),
                    ('Temporada Baja', '2026-04-20', '2026-12-23', 1);
            ");

            // Asignar tarifas existentes a Temporada Baja (TemporadaId = 0 aún)
            migrationBuilder.Sql(@"
                UPDATE Tarifas
                SET TemporadaId = (SELECT Id FROM Temporadas WHERE Nombre = 'Temporada Baja')
                WHERE TemporadaId = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Tarifas_AlojamientoId_CantidadPersonas_TemporadaId",
                table: "Tarifas",
                columns: new[] { "AlojamientoId", "CantidadPersonas", "TemporadaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tarifas_TemporadaId",
                table: "Tarifas",
                column: "TemporadaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tarifas_Temporadas_TemporadaId",
                table: "Tarifas",
                column: "TemporadaId",
                principalTable: "Temporadas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tarifas_Temporadas_TemporadaId",
                table: "Tarifas");

            migrationBuilder.DropTable(
                name: "Temporadas");

            migrationBuilder.DropIndex(
                name: "IX_Tarifas_AlojamientoId_CantidadPersonas_TemporadaId",
                table: "Tarifas");

            migrationBuilder.DropIndex(
                name: "IX_Tarifas_TemporadaId",
                table: "Tarifas");

            migrationBuilder.DropColumn(
                name: "TemporadaId",
                table: "Tarifas");

            migrationBuilder.CreateIndex(
                name: "IX_Tarifas_AlojamientoId_CantidadPersonas",
                table: "Tarifas",
                columns: new[] { "AlojamientoId", "CantidadPersonas" },
                unique: true);
        }
    }
}
