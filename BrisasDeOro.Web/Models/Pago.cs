namespace BrisasDeOro.Web.Models;

public enum TipoPago
{
    Sena,
    PagoParcial,
    Cancelacion,
    Ajuste
}

public enum MetodoPago
{
    Transferencia,
    Efectivo,
    TarjetaCredito,
    TarjetaDebito
}

public class Pago
{
    public int Id { get; set; }
    public int ReservaId { get; set; }
    public Reserva Reserva { get; set; } = null!;
    public TipoPago TipoPago { get; set; }
    public decimal Monto { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public DateTime Fecha { get; set; }
    public string? Observaciones { get; set; }
}
