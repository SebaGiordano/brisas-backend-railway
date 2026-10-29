using BrisasDeOro.Web.Models;
using BrisasDeOro.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrisasDeOro.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public UsuariosController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User);

        var users = await _userManager.Users
            .OrderBy(u => u.UserName)
            .ToListAsync();

        var vm = new List<UsuarioListItemViewModel>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            vm.Add(new UsuarioListItemViewModel
            {
                Id              = u.Id,
                UserName        = u.UserName!,
                Rol             = roles.FirstOrDefault() ?? "Sin rol",
                FechaCreacion   = u.FechaCreacion,
                Activo          = !(u.LockoutEnabled
                                    && u.LockoutEnd.HasValue
                                    && u.LockoutEnd > DateTimeOffset.UtcNow),
                PhoneNumber     = u.PhoneNumber,
                EsUsuarioActual = u.Id == currentUserId
            });
        }

        return View(vm);
    }

    // ── Crear GET ─────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Crear() => View(new CrearUsuarioViewModel());

    // ── Crear POST ────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearUsuarioViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName       = model.UserName,
            PhoneNumber    = model.PhoneNumber,
            FechaCreacion  = DateTime.Now,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.Rol);

        TempData["Mensaje"]     = $"Usuario '{user.UserName}' creado correctamente.";
        TempData["MensajeTipo"] = "success";
        return RedirectToAction(nameof(Index));
    }

    // ── Editar GET ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Editar(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return View(new EditarUsuarioViewModel
        {
            Id          = user.Id,
            UserName    = user.UserName!,
            PhoneNumber = user.PhoneNumber,
            Rol         = roles.FirstOrDefault() ?? "Viewer"
        });
    }

    // ── Editar POST ───────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EditarUsuarioViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        bool esUsuarioActual = user.Id == _userManager.GetUserId(User);

        // Cambio de nombre de usuario (si corresponde) primero: si falla (por
        // ejemplo, nombre duplicado), no se guarda ningún otro cambio del formulario.
        bool cambioUserName = !string.Equals(user.UserName, model.UserName, StringComparison.Ordinal);
        if (cambioUserName)
        {
            var resultUserName = await _userManager.SetUserNameAsync(user, model.UserName);
            if (!resultUserName.Succeeded)
            {
                foreach (var e in resultUserName.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }
        }

        // Cambio de contraseña (opcional) primero: si falla, no se guarda ningún
        // otro cambio del formulario (celular/rol).
        bool cambioPassword = !string.IsNullOrEmpty(model.NuevaPassword);
        if (cambioPassword)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resultPassword = await _userManager.ResetPasswordAsync(user, token, model.NuevaPassword!);
            if (!resultPassword.Succeeded)
            {
                foreach (var e in resultPassword.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }
        }

        user.PhoneNumber = model.PhoneNumber;
        await _userManager.UpdateAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(model.Rol))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Rol);
        }

        // Si el administrador se cambió a sí mismo el nombre de usuario y/o la
        // contraseña, refrescar su sesión actual para que no quede invalidada y
        // el nombre de arriba a la derecha se actualice.
        if ((cambioUserName || cambioPassword) && esUsuarioActual)
            await _signInManager.RefreshSignInAsync(user);

        TempData["Mensaje"] = (cambioUserName, cambioPassword) switch
        {
            (true,  true ) => $"Usuario actualizado. El nombre de usuario ahora es '{user.UserName}' y la contraseña fue cambiada.",
            (true,  false) => $"Usuario actualizado. El nombre de usuario ahora es '{user.UserName}'.",
            (false, true ) => $"Usuario '{user.UserName}' actualizado. La contraseña fue cambiada.",
            _              => $"Usuario '{user.UserName}' actualizado correctamente.",
        };
        TempData["MensajeTipo"] = "success";
        return RedirectToAction(nameof(Index));
    }

    // ── Desactivar POST ───────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(string id)
    {
        if (id == _userManager.GetUserId(User))
        {
            TempData["Mensaje"]     = "No podés desactivar tu propia cuenta.";
            TempData["MensajeTipo"] = "warning";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.LockoutEnabled = true;
        user.LockoutEnd     = DateTimeOffset.MaxValue;
        await _userManager.UpdateAsync(user);

        TempData["Mensaje"]     = $"Usuario '{user.UserName}' desactivado.";
        TempData["MensajeTipo"] = "warning";
        return RedirectToAction(nameof(Index));
    }

    // ── Activar POST ──────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.LockoutEnabled = false;
        user.LockoutEnd     = null;
        await _userManager.UpdateAsync(user);

        TempData["Mensaje"]     = $"Usuario '{user.UserName}' activado.";
        TempData["MensajeTipo"] = "success";
        return RedirectToAction(nameof(Index));
    }
}
