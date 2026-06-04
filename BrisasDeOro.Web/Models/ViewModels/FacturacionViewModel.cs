namespace BrisasDeOro.Web.Models.ViewModels;

// ── ViewModel raíz ────────────────────────────────────────────────────────────

public class FacturacionViewModel
{
    // Tab 1: Movimientos
    public List<MovimientoViewModel>     Movimientos      { get; set; } = new();
    public decimal                       TotalPeriodo     { get; set; }

    // Tab 2: Saldos pendientes
    public List<SaldoPendienteViewModel> SaldosPendientes { get; set; } = new();

    // Filtros activos (Tab 1)
    public string? FiltroTitular  { get; set; }
    public string? FiltroDesde    { get; set; }
    public string? FiltroHasta    { get; set; }
    public string? FiltroMetodo   { get; set; }
    public string? FiltroConcepto { get; set; }
    public string  FiltroOrden    { get; set; } = "desc";

    // Filtros activos (Tab 2)
    public string? FiltroTitularSaldos { get; set; }

    // Pestaña activa
    public string TabActiva { get; set; } = "movimientos";
}

// ── Movimiento (un pago) ──────────────────────────────────────────────────────

public class MovimientoViewModel
{
    public int        ReservaId         { get; set; }
    public DateTime   Fecha             { get; set; }
    public string     NombreHuesped     { get; set; } = string.Empty;
    public string     NombreAlojamiento { get; set; } = string.Empty;
    public TipoPago   TipoPago          { get; set; }
    public MetodoPago MetodoPago        { get; set; }
    public decimal    Monto             { get; set; }
    public string?    Observaciones     { get; set; }
}

// ── Saldo pendiente (una reserva con deuda) ───────────────────────────────────

public class SaldoPendienteViewModel
{
    public int      ReservaId         { get; set; }
    public string   NombreHuesped     { get; set; } = string.Empty;
    public string   NombreAlojamiento { get; set; } = string.Empty;
    public DateTime FechaIngreso      { get; set; }
    public DateTime FechaSalida       { get; set; }
    public decimal  MontoTotal        { get; set; }
    public decimal  TotalCobrado      { get; set; }

    public decimal SaldoPendiente  => MontoTotal - TotalCobrado;
    public string  EstadoPagoClase => TotalCobrado == 0 ? "pago-sin-sena" : "pago-senado";
    public string  EstadoPagoTexto => TotalCobrado == 0 ? "Sin seña"      : "Señado";
}
