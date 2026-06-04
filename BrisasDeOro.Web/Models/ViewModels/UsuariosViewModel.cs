using System.ComponentModel.DataAnnotations;

namespace BrisasDeOro.Web.Models.ViewModels;

// ── Ítem del listado ──────────────────────────────────────────────────────────

public class UsuarioListItemViewModel
{
    public string   Id              { get; set; } = string.Empty;
    public string   UserName        { get; set; } = string.Empty;
    public string   Rol             { get; set; } = string.Empty;
    public DateTime FechaCreacion   { get; set; }
    public bool     Activo          { get; set; }
    public string?  PhoneNumber     { get; set; }
    public bool     EsUsuarioActual { get; set; }
}

// ── Crear usuario ─────────────────────────────────────────────────────────────

public class CrearUsuarioViewModel
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [Display(Name = "Nombre de usuario")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmá la contraseña.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Número de celular")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Seleccioná un rol.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = "Viewer";
}

// ── Editar usuario ────────────────────────────────────────────────────────────

public class EditarUsuarioViewModel
{
    public string  Id       { get; set; } = string.Empty;

    [Display(Name = "Nombre de usuario")]
    public string  UserName { get; set; } = string.Empty;

    [Display(Name = "Número de celular")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Seleccioná un rol.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = string.Empty;
}
