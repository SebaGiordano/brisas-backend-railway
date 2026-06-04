namespace BrisasDeOro.Web.Models.ViewModels;

public class ReservaListItemViewModel
{
    public int Id { get; set; }
    public string NombreHuesped { get; set; } = string.Empty;
    public string NombreAlojamiento { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
    public DateTime FechaSalida { get; set; }
    public int CantidadHuespedes { get; set; }
    public bool IncluyeDesayuno { get; set; }
    public bool EsInvitacion { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal TotalCobrado { get; set; }
    public EstadoReserva Estado { get; set; }

    // Calculados
    public decimal SaldoPendiente => MontoTotal - TotalCobrado;
    public int CantidadNoches    => (FechaSalida - FechaIngreso).Days;

    public string EstadoPagoClase => EsInvitacion    ? "pago-invitacion"
        : TotalCobrado == 0                          ? "pago-sin-sena"
        : SaldoPendiente > 0                         ? "pago-senado"
                                                     : "pago-pagado";

    public string EstadoPagoTexto => EsInvitacion    ? "Invitación"
        : TotalCobrado == 0                          ? "Sin seña"
        : SaldoPendiente > 0                         ? "Señado"
                                                     : "Pagado";
}
