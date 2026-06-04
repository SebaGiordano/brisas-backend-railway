namespace BrisasDeOro.Web.Models;

public class Tarifa
{
    public int         Id                { get; set; }
    public int         AlojamientoId     { get; set; }
    public Alojamiento Alojamiento       { get; set; } = null!;
    public int         TemporadaId       { get; set; }
    public Temporada   Temporada         { get; set; } = null!;
    public int         CantidadPersonas  { get; set; }
    public decimal     PrecioConDesayuno { get; set; }
    public decimal     PrecioSinDesayuno { get; set; }
    public bool        Activo            { get; set; } = true;
}
