namespace BrisasDeOro.Web.Models.ViewModels;

// ── ViewModel raíz ────────────────────────────────────────────────────────────

public class FacturacionViewModel
{
    // Tab 1: Movimientos
    public List<MovimientoViewModel>     Movimientos      { get; set; } = new();
    public decimal                       TotalPeriodo     { get; set; }

    // Tab 1: Paginación (lista ya paginada arriba; estos totales son sobre todo lo filtrado)
    public int PaginaMovimientos       { get; set; } = 1;
    public int TotalPaginasMovimientos { get; set; }
    public int TotalMovimientos        { get; set; }

    // Tab 2: Saldos pendientes
    public List<SaldoPendienteViewModel> SaldosPendientes { get; set; } = new();

    // Tab 2: Paginación (lista ya paginada arriba; estos totales son sobre todo lo filtrado)
    public int     PaginaSaldos       { get; set; } = 1;
    public int     TotalPaginasSaldos { get; set; }
    public int     TotalSaldos        { get; set; }
    public decimal TotalSaldoPendiente { get; set; }

    // Filtros activos (Tab 1)
    public string? FiltroTitular  { get; set; }
    public string? FiltroDesde    { get; set; }
    public string? FiltroHasta    { get; set; }
    public string? FiltroMetodo   { get; set; }
    public string? FiltroConcepto { get; set; }
    public string  FiltroOrden    { get; set; } = "desc";

    // Filtros activos (Tab 2)
    public string? FiltroTitularSaldos { get; set; }
    public string? FiltroDesdeSaldos { get; set; }
    public string? FiltroHastaSaldos { get; set; }
    public string  FiltroOrdenSaldos { get; set; } = "asc";

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

    public bool                       EsGrupal  { get; set; }
    public List<UnidadGrupalResumen>  Unidades  { get; set; } = new();

    public string? VinculadaConNombre { get; set; }
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

    public bool                       EsGrupal  { get; set; }
    public List<UnidadGrupalResumen>  Unidades  { get; set; } = new();

    public decimal SaldoPendiente  => MontoTotal - TotalCobrado;
    public string  EstadoPagoClase => TotalCobrado == 0 ? "pago-sin-sena" : "pago-senado";
    public string  EstadoPagoTexto => TotalCobrado == 0 ? "Sin seña"      : "Señado";

    public string? VinculadaConNombre { get; set; }
}

// ── Unidad de alojamiento en una reserva grupal ───────────────────────────────

public class UnidadGrupalResumen
{
    public string Nombre   { get; set; } = string.Empty;
    public int    Personas { get; set; }
}
