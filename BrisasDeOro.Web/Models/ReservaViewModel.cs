using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BrisasDeOro.Web.Models;

public class ReservaViewModel
{
    [Required(ErrorMessage = "El nombre del huésped es obligatorio.")]
    [Display(Name = "Nombre del huésped")]
    public string NombreHuesped { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccioná un alojamiento.")]
    [Display(Name = "Alojamiento")]
    public int? AlojamientoId { get; set; }

    [Required(ErrorMessage = "La cantidad de personas es obligatoria.")]
    [Range(1, 100, ErrorMessage = "Ingresá una cantidad válida.")]
    [Display(Name = "Cantidad de personas")]
    public int? CantidadHuespedes { get; set; }

    [Display(Name = "¿Incluye desayuno?")]
    public bool IncluyeDesayuno { get; set; } = true;

    [Required(ErrorMessage = "La fecha de ingreso es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de ingreso")]
    public DateTime? FechaIngreso { get; set; }

    [Required(ErrorMessage = "La fecha de egreso es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de egreso")]
    public DateTime? FechaSalida { get; set; }

    [Display(Name = "Precio por día")]
    public decimal? PrecioPorDia { get; set; }

    [Display(Name = "Total de la estadía")]
    public decimal? MontoTotal { get; set; }

    [Display(Name = "¿Usa aire acondicionado adicional?")]
    public bool UsaAireAcondicionado { get; set; } = false;

    [Display(Name = "Precio de aire por día")]
    public decimal? PrecioAireDiario { get; set; }

    // Días de aire tildados por cabaña, enviados desde el formulario.
    public List<AireCabanaInputModel> AireCabanas { get; set; } = new();

    [Display(Name = "¿Es invitación?")]
    public bool EsInvitacion { get; set; } = false;

    [Phone(ErrorMessage = "Ingresá un teléfono válido.")]
    [Display(Name = "Teléfono de contacto")]
    public string? Telefono { get; set; }

    [Display(Name = "Canal de origen")]
    public string? CanalOrigen { get; set; }

    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    [Display(Name = "¿Es reserva grupal?")]
    public bool EsGrupal { get; set; } = false;

    // Unidades seleccionadas en modo grupal (populadas por JS antes del submit)
    public List<UnidadGrupalViewModel> UnidadesGrupales { get; set; } = new();

    // Reservas Vinculadas: IDs de las reservas que quedarán vinculadas a esta
    // (populado por JS antes del submit). Si viene vacío, no se toca el vínculo
    // existente salvo que EliminarVinculo sea true.
    public List<int> ReservasVinculadasIds { get; set; } = new();

    [Display(Name = "Etiqueta del grupo (opcional)")]
    public string? EtiquetaGrupoVinculado { get; set; }

    // Si es true, se elimina el vínculo actual de esta reserva (deja de pertenecer
    // a su GrupoVinculado, sin afectar a las demás reservas del grupo).
    public bool EliminarVinculo { get; set; } = false;

    // Solo para poblar el dropdown en la vista
    public List<SelectListItem> Alojamientos { get; set; } = new();
}

public class UnidadGrupalViewModel
{
    public int AlojamientoId { get; set; }
    public int CantidadHuespedes { get; set; }
}

public class AireCabanaInputModel
{
    public int AlojamientoId { get; set; }
    public List<DateTime> Fechas { get; set; } = new();
}

public class EditarReservaViewModel : ReservaViewModel
{
    public int Id { get; set; }
}
