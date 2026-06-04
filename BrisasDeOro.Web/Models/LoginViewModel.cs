using System.ComponentModel.DataAnnotations;

namespace BrisasDeOro.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
