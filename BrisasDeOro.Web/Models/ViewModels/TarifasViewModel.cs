using System.ComponentModel.DataAnnotations;

namespace BrisasDeOro.Web.Models.ViewModels;

public class TarifaListItemViewModel
{
    public int     Id                { get; set; }
    public int     CantidadPersonas  { get; set; }
    public decimal PrecioConDesayuno { get; set; }
    public decimal PrecioSinDesayuno { get; set; }
    public bool    Activo            { get; set; }
}

public class TarifaGrupoViewModel
{
    public int             AlojamientoId     { get; set; }
    public string          NombreAlojamiento { get; set; } = string.Empty;
    public TipoAlojamiento Tipo              { get; set; }
    public List<TarifaListItemViewModel> Tarifas { get; set; } = new();
}

public class TemporadaViewModel
{
    public int      Id          { get; set; }
    public string   Nombre      { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin    { get; set; }
    public bool     Activo      { get; set; }
}

public class TarifasIndexViewModel
{
    public List<TarifaGrupoViewModel> Grupos           { get; set; } = new();
    public List<TemporadaViewModel>   Temporadas        { get; set; } = new();
    public int                        TemporadaActivaId { get; set; }
}

public class TemporadaEditItem
{
    public int    Id          { get; set; }
    public string FechaInicio { get; set; } = string.Empty;
    public string FechaFin    { get; set; } = string.Empty;
}

public class TarifaEditItem
{
    public int     TarifaId          { get; set; }
    public int     AlojamientoId     { get; set; }
    public decimal PrecioConDesayuno { get; set; }
    public decimal PrecioSinDesayuno { get; set; }
}

public class EditarTarifaViewModel
{
    public int    Id                { get; set; }
    public string NombreAlojamiento { get; set; } = string.Empty;
    public string NombreTemporada   { get; set; } = string.Empty;
    public int    CantidadPersonas  { get; set; }

    [Required(ErrorMessage = "El precio con desayuno es obligatorio.")]
    [Range(0, 9999999, ErrorMessage = "El precio debe ser mayor o igual a 0.")]
    [Display(Name = "Precio con desayuno")]
    public decimal PrecioConDesayuno { get; set; }

    [Required(ErrorMessage = "El precio sin desayuno es obligatorio.")]
    [Range(0, 9999999, ErrorMessage = "El precio debe ser mayor o igual a 0.")]
    [Display(Name = "Precio sin desayuno")]
    public decimal PrecioSinDesayuno { get; set; }
}
