namespace BrisasDeOro.Web.Models;

public enum TipoAlojamiento
{
    Cabaña,
    Habitacion,
    Apart,
    Casa
}

public class Alojamiento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoAlojamiento Tipo { get; set; }
    public int Capacidad { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}