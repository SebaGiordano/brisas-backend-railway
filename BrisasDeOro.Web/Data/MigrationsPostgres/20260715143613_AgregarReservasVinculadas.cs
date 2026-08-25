using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BrisasDeOro.Web.Data.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AgregarReservasVinculadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GrupoVinculadoId",
                table: "Reservas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GruposVinculados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Etiqueta = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposVinculados", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_GrupoVinculadoId",
                table: "Reservas",
                column: "GrupoVinculadoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_GruposVinculados_GrupoVinculadoId",
                table: "Reservas",
                column: "GrupoVinculadoId",
                principalTable: "GruposVinculados",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_GruposVinculados_GrupoVinculadoId",
                table: "Reservas");

            migrationBuilder.DropTable(
                name: "GruposVinculados");

            migrationBuilder.DropIndex(
                name: "IX_Reservas_GrupoVinculadoId",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "GrupoVinculadoId",
                table: "Reservas");
        }
    }
}
