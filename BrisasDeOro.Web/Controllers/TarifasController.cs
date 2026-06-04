using System.Text.RegularExpressions;
using BrisasDeOro.Web.Data;
using BrisasDeOro.Web.Models;
using BrisasDeOro.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrisasDeOro.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class TarifasController : Controller
{
    private readonly ApplicationDbContext _context;

    public TarifasController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index(int? temporadaId)
    {
        var temporadas = await _context.Temporadas
            .OrderBy(t => t.FechaInicio)
            .ToListAsync();

        // Temporada activa: la seleccionada, o la que contiene hoy, o Temporada Baja, o la primera
        var hoy = DateTime.Today;
        var tempActiva = temporadaId.HasValue
            ? temporadas.FirstOrDefault(t => t.Id == temporadaId.Value)
            : temporadas.FirstOrDefault(t => hoy >= t.FechaInicio && hoy <= t.FechaFin)
              ?? temporadas.FirstOrDefault(t => t.Nombre == "Temporada Baja")
              ?? temporadas.FirstOrDefault();

        var grupos = new List<TarifaGrupoViewModel>();

        if (tempActiva != null)
        {
            var tarifas = await _context.Tarifas
                .Include(t => t.Alojamiento)
                .Where(t => t.TemporadaId == tempActiva.Id)
                .ToListAsync();

            grupos = tarifas
                .GroupBy(t => t.Alojamiento)
                .OrderBy(g => g.Key.Tipo == TipoAlojamiento.Habitacion ? 0
                            : g.Key.Tipo == TipoAlojamiento.Apart      ? 1 : 2)
                .ThenBy(g => NumeroEnNombre(g.Key.Nombre))
                .ThenBy(g => g.Key.Nombre)
                .Select(g => new TarifaGrupoViewModel
                {
                    AlojamientoId     = g.Key.Id,
                    NombreAlojamiento = g.Key.Nombre,
                    Tipo              = g.Key.Nombre is "Habitación 6" or "Habitación 10"
                                        ? TipoAlojamiento.Apart
                                        : g.Key.Tipo,
                    Tarifas           = g.OrderBy(t => t.CantidadPersonas)
                                         .Select(t => new TarifaListItemViewModel
                                         {
                                             Id                = t.Id,
                                             CantidadPersonas  = t.CantidadPersonas,
                                             PrecioConDesayuno = t.PrecioConDesayuno,
                                             PrecioSinDesayuno = t.PrecioSinDesayuno,
                                             Activo            = t.Activo
                                         }).ToList()
                })
                .ToList();
        }

        var vm = new TarifasIndexViewModel
        {
            Grupos           = grupos,
            TemporadaActivaId = tempActiva?.Id ?? 0,
            Temporadas       = temporadas.Select(t => new TemporadaViewModel
            {
                Id          = t.Id,
                Nombre      = t.Nombre,
                FechaInicio = t.FechaInicio,
                FechaFin    = t.FechaFin,
                Activo      = t.Activo
            }).ToList()
        };

        ViewBag.UltimoAjuste = Request.Cookies["ultimo_ajuste_tarifas"];
        return View(vm);
    }

    // ── GuardarTemporadas POST ────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarTemporadas(List<TemporadaEditItem> items)
    {
        foreach (var item in items)
        {
            var t = await _context.Temporadas.FindAsync(item.Id);
            if (t == null) continue;
            if (DateTime.TryParse(item.FechaInicio, out var fi)) t.FechaInicio = fi;
            if (DateTime.TryParse(item.FechaFin,    out var ff)) t.FechaFin    = ff;
        }
        await _context.SaveChangesAsync();
        TempData["Mensaje"]     = "Fechas de temporadas actualizadas.";
        TempData["MensajeTipo"] = "success";
        return RedirectToAction(nameof(Index));
    }

    // ── AjustarPrecios POST ───────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AjustarPrecios(
        decimal porcentaje, string tipoAloj, string servicio, int temporadaId)
    {
        if (porcentaje == 0)
        {
            TempData["Mensaje"]     = "El porcentaje no puede ser 0.";
            TempData["MensajeTipo"] = "warning";
            return RedirectToAction(nameof(Index), new { temporadaId });
        }

        var tarifas = await _context.Tarifas
            .Include(t => t.Alojamiento)
            .Where(t => t.TemporadaId == temporadaId)
            .ToListAsync();

        if (tipoAloj != "todos")
        {
            var tipoEnum = tipoAloj switch
            {
                "habitacion" => TipoAlojamiento.Habitacion,
                "cabaña"     => TipoAlojamiento.Cabaña,
                "apart"      => TipoAlojamiento.Apart,
                _            => (TipoAlojamiento?)null
            };
            if (tipoEnum.HasValue)
                tarifas = tarifas.Where(t => t.Alojamiento.Tipo == tipoEnum.Value).ToList();
        }

        var factor = 1m + porcentaje / 100m;

        foreach (var t in tarifas)
        {
            if (servicio != "sin-desayuno" && t.PrecioConDesayuno > 0)
                t.PrecioConDesayuno = Math.Round(t.PrecioConDesayuno * factor, 0);
            if (servicio != "con-desayuno" && t.PrecioSinDesayuno > 0)
                t.PrecioSinDesayuno = Math.Round(t.PrecioSinDesayuno * factor, 0);
        }

        await _context.SaveChangesAsync();

        // ── Registro de último ajuste (cookie 90 días) ────────────────────────
        var tipoLabel = tipoAloj switch
        {
            "todos"      => "Todas las unidades",
            "habitacion" => "Habitaciones",
            "cabaña"     => "Cabañas",
            "apart"      => "Aparts",
            _            => tipoAloj
        };
        var servicioLabel = servicio switch
        {
            "ambos"        => "Ambos servicios",
            "con-desayuno" => "Con desayuno",
            "sin-desayuno" => "Sin desayuno",
            _              => servicio
        };
        var tempNombre = (await _context.Temporadas.FindAsync(temporadaId))?.Nombre ?? "";

        var signo      = porcentaje > 0 ? "+" : "";
        var pctDisplay = porcentaje % 1 == 0
            ? ((int)porcentaje).ToString()
            : porcentaje.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        var fechaStr   = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var cookieVal  = $"{signo}{pctDisplay}%|{servicioLabel}|{tipoLabel}|{tempNombre}|{fechaStr}";

        Response.Cookies.Append("ultimo_ajuste_tarifas", cookieVal, new CookieOptions
        {
            MaxAge      = TimeSpan.FromDays(90),
            IsEssential = true,
            SameSite    = SameSiteMode.Lax
        });

        TempData["Mensaje"]     = $"Ajuste aplicado: {signo}{pctDisplay}% en {tarifas.Count} tarifa(s) de {tempNombre}.";
        TempData["MensajeTipo"] = "success";
        return RedirectToAction(nameof(Index), new { temporadaId });
    }

    // ── EditarGrupo POST ──────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarGrupo(
        List<TarifaEditItem> filas, int temporadaId, int cantPersonas)
    {
        int updated = 0, created = 0;

        foreach (var fila in filas)
        {
            if (fila.TarifaId > 0)
            {
                var tarifa = await _context.Tarifas.FindAsync(fila.TarifaId);
                if (tarifa != null)
                {
                    tarifa.PrecioConDesayuno = fila.PrecioConDesayuno;
                    tarifa.PrecioSinDesayuno = fila.PrecioSinDesayuno;
                    updated++;
                }
            }
            else
            {
                _context.Tarifas.Add(new Tarifa
                {
                    AlojamientoId     = fila.AlojamientoId,
                    TemporadaId       = temporadaId,
                    CantidadPersonas  = cantPersonas,
                    PrecioConDesayuno = fila.PrecioConDesayuno,
                    PrecioSinDesayuno = fila.PrecioSinDesayuno,
                    Activo            = true
                });
                created++;
            }
        }

        await _context.SaveChangesAsync();

        TempData["Mensaje"] = (updated, created) switch
        {
            ( > 0,  > 0) => $"Se actualizaron {updated} y se crearon {created} tarifa(s).",
            ( > 0,   _ ) => $"Se actualizaron {updated} tarifa(s).",
            _            => $"Se crearon {created} tarifa(s)."
        };
        TempData["MensajeTipo"] = "success";
        return RedirectToAction(nameof(Index), new { temporadaId });
    }

    // ── Editar GET ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var tarifa = await _context.Tarifas
            .Include(t => t.Alojamiento)
            .Include(t => t.Temporada)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tarifa == null) return NotFound();

        return View(new EditarTarifaViewModel
        {
            Id                = tarifa.Id,
            NombreAlojamiento = tarifa.Alojamiento.Nombre,
            NombreTemporada   = tarifa.Temporada.Nombre,
            CantidadPersonas  = tarifa.CantidadPersonas,
            PrecioConDesayuno = tarifa.PrecioConDesayuno,
            PrecioSinDesayuno = tarifa.PrecioSinDesayuno
        });
    }

    // ── Editar POST ───────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EditarTarifaViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var tarifa = await _context.Tarifas.FindAsync(model.Id);
        if (tarifa == null) return NotFound();

        tarifa.PrecioConDesayuno = model.PrecioConDesayuno;
        tarifa.PrecioSinDesayuno = model.PrecioSinDesayuno;

        await _context.SaveChangesAsync();

        TempData["Mensaje"]     = $"Tarifa de {model.NombreAlojamiento} — {model.NombreTemporada} ({model.CantidadPersonas} {(model.CantidadPersonas == 1 ? "persona" : "personas")}) actualizada.";
        TempData["MensajeTipo"] = "success";
        return RedirectToAction(nameof(Index));
    }

    private static int NumeroEnNombre(string nombre)
    {
        var m = Regex.Match(nombre, @"\d+");
        return m.Success ? int.Parse(m.Value) : 0;
    }
}
