using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BrisasDeOro.Web.Data.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AgregarReservaGrupal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsGrupal",
                table: "Reservas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ReservaAlojamientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReservaId = table.Column<int>(type: "integer", nullable: false),
                    AlojamientoId = table.Column<int>(type: "integer", nullable: false),
                    CantidadHuespedes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservaAlojamientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservaAlojamientos_Alojamientos_AlojamientoId",
                        column: x => x.AlojamientoId,
                        principalTable: "Alojamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservaAlojamientos_Reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "Reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservaAlojamientos_AlojamientoId",
                table: "ReservaAlojamientos",
                column: "AlojamientoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservaAlojamientos_ReservaId",
                table: "ReservaAlojamientos",
                column: "ReservaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservaAlojamientos");

            migrationBuilder.DropColumn(
                name: "EsGrupal",
                table: "Reservas");
        }
    }
}
