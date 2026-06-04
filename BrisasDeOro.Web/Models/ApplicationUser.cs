using Microsoft.AspNetCore.Identity;

namespace BrisasDeOro.Web.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}
