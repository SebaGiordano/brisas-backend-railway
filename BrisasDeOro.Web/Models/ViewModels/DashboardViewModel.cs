namespace BrisasDeOro.Web.Models.ViewModels;

public class DashboardViewModel
{
    // ── Filtros ────────────────────────────────────────────────────────────────
    public int?      Mes       { get; set; }
    public int?      Anno      { get; set; }
    public DateTime? Desde     { get; set; }
    public DateTime? Hasta     { get; set; }
    public bool      Comparar  { get; set; }
    public int?      MesComp   { get; set; }
    public int?      AnnoComp  { get; set; }
    public DateTime? DesdeComp { get; set; }
    public DateTime? HastaComp { get; set; }

    public List<int> AnnosDisponibles { get; set; } = new();

    // ── Datos ─────────────────────────────────────────────────────────────────
    public DashboardPeriodoViewModel  Periodo            { get; set; } = new();
    public DashboardPeriodoViewModel? PeriodoComparacion { get; set; }
}

public class DashboardPeriodoViewModel
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public string   Label { get; set; } = "";

    // Tarjetas de resumen
    public decimal TotalIngresos    { get; set; }
    public decimal PctOcupacion     { get; set; }
    public int     CantidadReservas { get; set; }
    public decimal PromedioNoches   { get; set; }

    // Ingresos por método de pago (dos variantes)
    public IngresoMetodoPagoViewModel  IngresosFechaIngreso { get; set; } = new();
    public IngresoMetodoPagoViewModel  IngresosProrrateo    { get; set; } = new();

    // Ocupación
    public OcupacionDetalleViewModel   Ocupacion            { get; set; } = new();

    // Canales de origen
    public List<CanalReservaViewModel> Canales              { get; set; } = new();
}

public class IngresoMetodoPagoViewModel
{
    public decimal Efectivo       { get; set; }
    public decimal Transferencia  { get; set; }
    public decimal TarjetaCredito { get; set; }
    public decimal TarjetaDebito  { get; set; }
    public decimal Total          => Efectivo + Transferencia + TarjetaCredito + TarjetaDebito;
}

public class OcupacionDetalleViewModel
{
    // Ocupación de unidades (días × unidad)
    public decimal PctHabitaciones   { get; set; }
    public decimal PctCabanas        { get; set; }
    public decimal PctTotal          { get; set; }
    public int     DiasHabOcupados   { get; set; }
    public int     DiasCabOcupados   { get; set; }
    public int     DiasHabTotal      { get; set; }
    public int     DiasCabTotal      { get; set; }
    public int     DiasTotalOcupados => DiasHabOcupados + DiasCabOcupados;
    public int     DiasTotal         => DiasHabTotal    + DiasCabTotal;

    // Ocupación de plazas (huéspedes reales vs capacidad máxima de unidades ocupadas)
    public decimal PctPlazasHabitaciones  { get; set; }
    public decimal PctPlazasCabanas       { get; set; }
    public decimal PctPlazasTotal         { get; set; }
    public int     PlazasHabOcupadas      { get; set; }
    public int     PlazasHabDisponibles   { get; set; }
    public int     PlazasCabOcupadas      { get; set; }
    public int     PlazasCabDisponibles   { get; set; }
    public int     PlazasTotalOcupadas    => PlazasHabOcupadas    + PlazasCabOcupadas;
    public int     PlazasTotalDisponibles => PlazasHabDisponibles + PlazasCabDisponibles;
}

public class CanalReservaViewModel
{
    public string  Canal      { get; set; } = "";
    public int     Cantidad   { get; set; }
    public decimal Porcentaje { get; set; }
}
