namespace BrisasDeOro.Web.Models;

public class Temporada
{
    public int      Id          { get; set; }
    public string   Nombre      { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin    { get; set; }
    public bool     Activo      { get; set; } = true;
}
