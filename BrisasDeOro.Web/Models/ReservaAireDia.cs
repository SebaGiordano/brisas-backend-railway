namespace BrisasDeOro.Web.Models;

public class ReservaAireDia
{
    public int Id { get; set; }

    public int ReservaId { get; set; }
    public Reserva Reserva { get; set; } = null!;

    public int AlojamientoId { get; set; }
    public Alojamiento Alojamiento { get; set; } = null!;

    public DateTime Fecha { get; set; }
}
