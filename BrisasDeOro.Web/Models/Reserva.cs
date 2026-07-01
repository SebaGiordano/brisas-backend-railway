namespace BrisasDeOro.Web.Models;

public enum EstadoReserva
{
    Confirmada,
    Cancelada,
    Finalizada
}

public class Reserva
{
    public int Id { get; set; }
    public int AlojamientoId { get; set; }
    public Alojamiento Alojamiento { get; set; } = null!;
    public string NombreHuesped { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public DateTime FechaIngreso { get; set; }
    public DateTime FechaSalida { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal MontoSena { get; set; }
    public EstadoReserva Estado { get; set; } = EstadoReserva.Confirmada;
    public bool EsInvitacion { get; set; } = false;
    public bool IncluyeDesayuno { get; set; } = false;
    public int CantidadHuespedes { get; set; }
    public string? CanalOrigen { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaCarga { get; set; } = DateTime.Now;

    // Reserva grupal: AlojamientoId/Alojamiento sigue apuntando a la primera unidad
    // tildada (compatibilidad con Calendario/Dashboard/Facturación, que todavía asumen
    // una sola unidad por reserva). El detalle completo vive en UnidadesGrupales.
    public bool EsGrupal { get; set; } = false;
    public List<ReservaAlojamiento> UnidadesGrupales { get; set; } = new();

    public List<Pago> Pagos { get; set; } = new();
}