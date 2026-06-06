namespace BrisasDeOro.Web.Models.ViewModels;

public class InicioViewModel
{
    public DateOnly Hoy    { get; init; }
    public DateOnly Manana { get; init; }

    // Hoy
    public List<MovimientoItem>     CheckInsHoy            { get; init; } = new();
    public List<MovimientoItem>     CheckOutsHoy           { get; init; } = new();
    public int                      PlazasOcupadasHoy        { get; init; }
    public int                      ComensalesDesayunoHoy    { get; init; }
    public int                      ComensalesDesayunoManana { get; init; }
    public List<string>             UnidadesLibresHoy      { get; init; } = new();
    public List<string>             UnidadesOcupadasHoy    { get; init; } = new();
    public List<LimpiezaItem>       LimpiezaDelDia         { get; init; } = new();
    public List<PendienteCobroItem> PendientesCobro        { get; init; } = new();

    // Mañana
    public List<MovimientoItem> CheckInsManana  { get; init; } = new();
    public List<MovimientoItem> CheckOutsManana { get; init; } = new();
}

public class MovimientoItem
{
    public string NombreHuesped     { get; init; } = string.Empty;
    public string NombreAlojamiento { get; init; } = string.Empty;
    public int    ReservaId         { get; init; }
}

public class LimpiezaItem
{
    public string       NombreAlojamiento { get; init; } = string.Empty;
    public List<string> Tareas            { get; init; } = new();
}

public class PendienteCobroItem
{
    public int      ReservaId         { get; init; }
    public string   NombreHuesped     { get; init; } = string.Empty;
    public string   NombreAlojamiento { get; init; } = string.Empty;
    public DateOnly FechaSalida       { get; init; }
    public decimal  SaldoPendiente    { get; init; }
    public bool     EsHoy             { get; init; }
}
