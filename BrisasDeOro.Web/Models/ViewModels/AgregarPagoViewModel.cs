using System.ComponentModel.DataAnnotations;

namespace BrisasDeOro.Web.Models.ViewModels;

public class AgregarPagoViewModel
{
    // ── Datos de la reserva (solo lectura, para mostrar en la vista) ──────────
    public int    ReservaId         { get; set; }
    public string NombreHuesped     { get; set; } = string.Empty;
    public string NombreAlojamiento { get; set; } = string.Empty;
    public DateTime FechaIngreso    { get; set; }
    public DateTime FechaSalida     { get; set; }
    public decimal MontoTotal       { get; set; }
    public decimal TotalCobrado     { get; set; }
    public decimal SaldoPendiente   { get; set; }

    // ── Campos del nuevo pago ─────────────────────────────────────────────────
    [Required(ErrorMessage = "Seleccioná el concepto del pago.")]
    [Display(Name = "Concepto")]
    public TipoPago TipoPago { get; set; } = TipoPago.Sena;

    [Required(ErrorMessage = "El monto es obligatorio.")]
    [Display(Name = "Monto")]
    public decimal Monto { get; set; }

    [Required(ErrorMessage = "Seleccioná el método de pago.")]
    [Display(Name = "Método de pago")]
    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha")]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }
}
