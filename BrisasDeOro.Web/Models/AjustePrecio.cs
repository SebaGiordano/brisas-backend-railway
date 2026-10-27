namespace BrisasDeOro.Web.Models;

// Registro de cada ajuste masivo de precios, con la foto de los precios antes y
// después, para poder deshacerlo restaurando los valores exactos.
public class AjustePrecio
{
    public int       Id            { get; set; }
    public int       TemporadaId   { get; set; }
    public Temporada Temporada     { get; set; } = null!;
    public decimal   Porcentaje    { get; set; }
    public string    TipoAloj      { get; set; } = string.Empty; // valor del select: todos, habitacion, cabaña, apart, casa
    public string    Servicio      { get; set; } = string.Empty; // ambos, con-desayuno, sin-desayuno
    public DateTime  Fecha         { get; set; }
    public List<AjustePrecioDetalle> Detalles { get; set; } = new();
}

public class AjustePrecioDetalle
{
    public int          Id                        { get; set; }
    public int          AjustePrecioId            { get; set; }
    public AjustePrecio AjustePrecio              { get; set; } = null!;
    public int          TarifaId                  { get; set; }
    public decimal      PrecioConDesayunoAntes    { get; set; }
    public decimal      PrecioSinDesayunoAntes    { get; set; }
    public decimal      PrecioConDesayunoDespues  { get; set; }
    public decimal      PrecioSinDesayunoDespues  { get; set; }
}
