using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BrisasDeOro.Web.Data.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AgregarHistorialAjustesPrecio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AjustesPrecio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemporadaId = table.Column<int>(type: "integer", nullable: false),
                    Porcentaje = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    TipoAloj = table.Column<string>(type: "text", nullable: false),
                    Servicio = table.Column<string>(type: "text", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AjustesPrecio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AjustesPrecio_Temporadas_TemporadaId",
                        column: x => x.TemporadaId,
                        principalTable: "Temporadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AjustesPrecioDetalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AjustePrecioId = table.Column<int>(type: "integer", nullable: false),
                    TarifaId = table.Column<int>(type: "integer", nullable: false),
                    PrecioConDesayunoAntes = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecioSinDesayunoAntes = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecioConDesayunoDespues = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecioSinDesayunoDespues = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AjustesPrecioDetalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AjustesPrecioDetalle_AjustesPrecio_AjustePrecioId",
                        column: x => x.AjustePrecioId,
                        principalTable: "AjustesPrecio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AjustesPrecio_TemporadaId_Fecha",
                table: "AjustesPrecio",
                columns: new[] { "TemporadaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_AjustesPrecioDetalle_AjustePrecioId",
                table: "AjustesPrecioDetalle",
                column: "AjustePrecioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AjustesPrecioDetalle");

            migrationBuilder.DropTable(
                name: "AjustesPrecio");
        }
    }
}
