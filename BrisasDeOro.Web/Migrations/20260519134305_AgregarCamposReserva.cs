using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrisasDeOro.Web.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposReserva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanalOrigen",
                table: "Reservas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CantidadHuespedes",
                table: "Reservas",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanalOrigen",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "CantidadHuespedes",
                table: "Reservas");
        }
    }
}
