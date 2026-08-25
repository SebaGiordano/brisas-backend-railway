using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BrisasDeOro.Web.Data.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AgregarAireAcondicionadoAdicional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrecioAireDiario",
                table: "Reservas",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReservaAireDias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReservaId = table.Column<int>(type: "integer", nullable: false),
                    AlojamientoId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservaAireDias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservaAireDias_Alojamientos_AlojamientoId",
                        column: x => x.AlojamientoId,
                        principalTable: "Alojamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservaAireDias_Reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "Reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservaAireDias_AlojamientoId",
                table: "ReservaAireDias",
                column: "AlojamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservaAireDias_ReservaId_AlojamientoId_Fecha",
                table: "ReservaAireDias",
                columns: new[] { "ReservaId", "AlojamientoId", "Fecha" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservaAireDias");

            migrationBuilder.DropColumn(
                name: "PrecioAireDiario",
                table: "Reservas");
        }
    }
}
