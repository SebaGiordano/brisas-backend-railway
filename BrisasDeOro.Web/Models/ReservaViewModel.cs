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

    [Display(Name = "¿Es invitación?")]
    public bool EsInvitacion { get; set; } = false;

    [Phone(ErrorMessage = "Ingresá un teléfono válido.")]
    [Display(Name = "Teléfono de contacto")]
    public string? Telefono { get; set; }

    [Display(Name = "Canal de origen")]
    public string? CanalOrigen { get; set; }

    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    // Solo para poblar el dropdown en la vista
    public List<SelectListItem> Alojamientos { get; set; } = new();
}

public class EditarReservaViewModel : ReservaViewModel
{
    public int Id { get; set; }
}
