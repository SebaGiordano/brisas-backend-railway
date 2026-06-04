using System.ComponentModel.DataAnnotations;

namespace BrisasDeOro.Web.Models.ViewModels;

public class AlojamientoListItemViewModel
{
    public int              Id         { get; set; }
    public string           Nombre     { get; set; } = string.Empty;
    public TipoAlojamiento  Tipo       { get; set; }
    public int              Capacidad  { get; set; }
    public bool             Activo     { get; set; }
}

public class EditarAlojamientoViewModel
{
    public int             Id        { get; set; }
    public TipoAlojamiento Tipo      { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La capacidad es obligatoria.")]
    [Range(1, 100, ErrorMessage = "La capacidad debe estar entre 1 y 100.")]
    [Display(Name = "Capacidad máxima")]
    public int Capacidad { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}
