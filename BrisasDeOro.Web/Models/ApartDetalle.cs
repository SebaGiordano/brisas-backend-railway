namespace BrisasDeOro.Web.Models;

public class ApartDetalle
{
    public int Id { get; set; }
    public int AlojamientoApartId { get; set; }
    public Alojamiento AlojamientoApart { get; set; } = null!;
    public int AlojamientoHab1Id { get; set; }
    public Alojamiento AlojamientoHab1 { get; set; } = null!;
    public int AlojamientoHab2Id { get; set; }
    public Alojamiento AlojamientoHab2 { get; set; } = null!;
}