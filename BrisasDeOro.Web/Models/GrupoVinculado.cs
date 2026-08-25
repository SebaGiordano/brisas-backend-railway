namespace BrisasDeOro.Web.Models;

// Agrupa 2 o más reservas independientes (cada una con su propia facturación,
// fechas y pagos) bajo un mismo vínculo, sin fusionarlas. Se usa tanto para el
// caso de un huésped que se mudó de alojamiento durante su estadía, como para
// varias reservas de un mismo evento/grupo familiar que se cobran por separado.
public class GrupoVinculado
{
    public int Id { get; set; }

    // Opcional: nombre descriptivo para identificar el grupo (ej. "Grupo Sole").
    // Si está vacío, la interfaz muestra un texto automático en su lugar
    // (según el titular de cada reserva vinculada).
    public string? Etiqueta { get; set; }

    public List<Reserva> Reservas { get; set; } = new();
}
