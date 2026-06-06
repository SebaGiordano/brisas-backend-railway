using System.Text.RegularExpressions;
using BrisasDeOro.Web.Data;
using BrisasDeOro.Web.Models;
using BrisasDeOro.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrisasDeOro.Web.Controllers;

[Authorize]
public class AlojamientosController : Controller
{
    private readonly ApplicationDbContext _context;

    public AlojamientosController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var alojamientos = await _context.Alojamientos
            .Select(a => new AlojamientoListItemViewModel
            {
                Id        = a.Id,
                Nombre    = a.Nombre,
                Tipo      = a.Tipo,
                Capacidad = a.Capacidad,
                Activo    = a.Activo
            })
            .ToListAsync();

        alojamientos = alojamientos
            .OrderBy(a => a.Tipo == TipoAlojamiento.Habitacion ? 0
                        : a.Tipo == TipoAlojamiento.Apart      ? 1 : 2)
            .ThenBy(a => NumeroEnNombre(a.Nombre))
            .ThenBy(a => a.Nombre)
            .ToList();

        return View(alojamientos);
    }

    private static int NumeroEnNombre(string nombre)
    {
        var m = Regex.Match(nombre, @"\d+");
        return m.Success ? int.Parse(m.Value) : 0;
    }

    // ── Editar GET ────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar(int id)
    {
        var a = await _context.Alojamientos.FindAsync(id);
        if (a == null) return NotFound();

        return View(new EditarAlojamientoViewModel
        {
            Id        = a.Id,
            Tipo      = a.Tipo,
            Nombre    = a.Nombre,
            Capacidad = a.Capacidad,
            Activo    = a.Activo
        });
    }

    // ── Editar POST ───────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar(EditarAlojamientoViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var a = await _context.Alojamientos.FindAsync(model.Id);
        if (a == null) return NotFound();

        a.Nombre    = model.Nombre;
        a.Capacidad = model.Capacidad;
        a.Activo    = model.Activo;
        // Tipo no se modifica

        await _context.SaveChangesAsync();

        TempData["Mensaje"]     = $"Alojamiento '{a.Nombre}' actualizado correctamente.";
        TempData["MensajeTipo"] = "success";
        return RedirectToAction(nameof(Index));
    }

    // ── Desactivar POST ───────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Desactivar(int id)
    {
        var a = await _context.Alojamientos.FindAsync(id);
        if (a == null) return NotFound();

        a.Activo = false;
        await _context.SaveChangesAsync();

        TempData["Mensaje"]     = $"Alojamiento '{a.Nombre}' desactivado.";
        TempData["MensajeTipo"] = "warning";
        return RedirectToAction(nameof(Index));
    }

    // ── Activar POST ──────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Activar(int id)
    {
        var a = await _context.Alojamientos.FindAsync(id);
        if (a == null) return NotFound();

        a.Activo = true;
        await _context.SaveChangesAsync();

        TempData["Mensaje"]     = $"Alojamiento '{a.Nombre}' activado.";
        TempData["MensajeTipo"] = "success";
        return RedirectToAction(nameof(Index));
    }
}
