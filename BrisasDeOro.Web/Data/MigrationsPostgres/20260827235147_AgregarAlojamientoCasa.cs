using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrisasDeOro.Web.Data.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AgregarAlojamientoCasa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tipo = 3 corresponde a TipoAlojamiento.Casa (Cabaña=0, Habitacion=1, Apart=2, Casa=3).
            migrationBuilder.InsertData(
                table: "Alojamientos",
                columns: new[] { "Nombre", "Tipo", "Capacidad", "Descripcion", "Activo" },
                values: new object[] { "Casa (Icho Cruz)", 3, 10, null, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Alojamientos",
                keyColumn: "Nombre",
                keyValue: "Casa (Icho Cruz)");
        }
    }
}
