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
    public EstadoReserva Estado            { get; set; }
    public bool          EsGrupal          { get; set; }
    public int           CantUnidadesGrupales { get; set; }

    // Null si esta reserva coincide directamente con la búsqueda; si no es null,
    // significa que apareció en los resultados solo por estar vinculada a la
    // reserva/nombre indicado (búsqueda expandida por Reservas Vinculadas).
    public string? VinculadaConNombre { get; set; }

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
