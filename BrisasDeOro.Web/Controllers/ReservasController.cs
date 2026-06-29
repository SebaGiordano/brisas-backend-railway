using BrisasDeOro.Web.Data;
using BrisasDeOro.Web.Models;
using BrisasDeOro.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BrisasDeOro.Web.Controllers;

[Authorize]
public class ReservasController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReservasController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index(
        string? busqueda, string? estado, string? estadoPago,
        DateTime? desde, DateTime? hasta,
        int pagina = 1)
    {
        const int porPagina = 20;

        estado ??= "activas";

        var hoy   = DateTime.Today;
        var query = _context.Reservas.AsQueryable();

        // ── Filtro por estado de reserva ──────────────────────────────────────
        query = estado switch
        {
            "proximas"   => query.Where(r => r.Estado != EstadoReserva.Cancelada
                                          && r.FechaIngreso > hoy),
            "en-curso"   => query.Where(r => r.Estado != EstadoReserva.Cancelada
                                          && r.FechaIngreso <= hoy
                                          && r.FechaSalida  >= hoy),
            "historicas" => query.Where(r => r.Estado != EstadoReserva.Cancelada
                                          && r.FechaSalida < hoy),
            "Cancelada"  => query.Where(r => r.Estado == EstadoReserva.Cancelada),
            "todas"      => query,
            _            => query.Where(r => r.Estado != EstadoReserva.Cancelada)
        };

        // ── Filtro por nombre ─────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(r => r.NombreHuesped.Contains(busqueda));

        // ── Filtro por rango de fechas de ingreso ─────────────────────────────
        if (desde.HasValue)
            query = query.Where(r => r.FechaIngreso >= desde.Value.Date);

        if (hasta.HasValue)
            query = query.Where(r => r.FechaIngreso <= hasta.Value.Date);

        // ── Filtro por estado de pago ─────────────────────────────────────────
        query = estadoPago switch
        {
            "sin-sena"   => query.Where(r => !r.EsInvitacion && !r.Pagos.Any()),
            "senado"     => query.Where(r => !r.EsInvitacion
                                          && r.Pagos.Any()
                                          && r.Pagos.Sum(p => p.Monto) < r.MontoTotal),
            "saldado"    => query.Where(r => !r.EsInvitacion
                                          && r.Pagos.Sum(p => p.Monto) >= r.MontoTotal),
            "invitacion" => query.Where(r => r.EsInvitacion),
            _            => query
        };

        var totalItems   = await query.CountAsync();
        var totalPaginas = (int)Math.Ceiling(totalItems / (double)porPagina);
        pagina = Math.Clamp(pagina, 1, Math.Max(1, totalPaginas));

        var reservas = await query
            .OrderBy(r => r.FechaIngreso)
            .Skip((pagina - 1) * porPagina)
            .Take(porPagina)
            .Select(r => new ReservaListItemViewModel
            {
                Id                = r.Id,
                NombreHuesped     = r.NombreHuesped,
                NombreAlojamiento = r.Alojamiento.Nombre,
                FechaIngreso      = r.FechaIngreso,
                FechaSalida       = r.FechaSalida,
                CantidadHuespedes = r.CantidadHuespedes,
                IncluyeDesayuno   = r.IncluyeDesayuno,
                EsInvitacion      = r.EsInvitacion,
                MontoTotal        = r.MontoTotal,
                TotalCobrado      = r.Pagos.Sum(p => p.Monto),
                Estado            = r.Estado
            })
            .ToListAsync();

        ViewBag.Busqueda     = busqueda    ?? string.Empty;
        ViewBag.Estado       = estado;
        ViewBag.EstadoPago   = estadoPago  ?? string.Empty;
        ViewBag.Desde        = desde?.ToString("yyyy-MM-dd") ?? string.Empty;
        ViewBag.Hasta        = hasta?.ToString("yyyy-MM-dd") ?? string.Empty;
        ViewBag.Pagina       = pagina;
        ViewBag.TotalPaginas = totalPaginas;
        ViewBag.TotalItems   = totalItems;

        return View(reservas);
    }

    // ── Details ───────────────────────────────────────────────────────────────

    public async Task<IActionResult> Details(int id)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Alojamiento)
            .Include(r => r.Pagos.OrderBy(p => p.Fecha))
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva == null) return NotFound();

        return View(reserva);
    }

    // ── Editar GET ────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar(int id)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Alojamiento)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva == null) return NotFound();

        int     noches       = (reserva.FechaSalida - reserva.FechaIngreso).Days;
        decimal precioPorDia = (noches > 0 && !reserva.EsInvitacion)
                                ? Math.Round(reserva.MontoTotal / noches, 2)
                                : 0;

        var vm = new EditarReservaViewModel
        {
            Id                = reserva.Id,
            NombreHuesped     = reserva.NombreHuesped,
            AlojamientoId     = reserva.AlojamientoId,
            CantidadHuespedes = reserva.CantidadHuespedes,
            IncluyeDesayuno   = reserva.IncluyeDesayuno,
            FechaIngreso      = reserva.FechaIngreso,
            FechaSalida       = reserva.FechaSalida,
            PrecioPorDia      = precioPorDia,
            MontoTotal        = reserva.MontoTotal,
            EsInvitacion      = reserva.EsInvitacion,
            Telefono          = reserva.Telefono,
            CanalOrigen       = reserva.CanalOrigen,
            Observaciones     = reserva.Observaciones
        };

        await PopularViewModel(vm);
        return View(vm);
    }

    // ── Editar POST ───────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar(EditarReservaViewModel model)
    {
        // ── Validaciones de negocio ───────────────────────────────────────────
        if (model.FechaIngreso.HasValue && model.FechaSalida.HasValue
            && model.FechaSalida.Value <= model.FechaIngreso.Value)
            ModelState.AddModelError("FechaSalida",
                "La fecha de egreso debe ser posterior a la de ingreso.");

        if (model.AlojamientoId.HasValue && model.CantidadHuespedes.HasValue)
        {
            var aloj = await _context.Alojamientos.FindAsync(model.AlojamientoId.Value);
            if (aloj != null && model.CantidadHuespedes.Value > aloj.Capacidad)
                ModelState.AddModelError("CantidadHuespedes",
                    $"Supera la capacidad máxima de {aloj.Capacidad} personas.");
        }

        if (!model.EsInvitacion && (!model.PrecioPorDia.HasValue || model.PrecioPorDia <= 0))
            ModelState.AddModelError("PrecioPorDia", "El precio por día es obligatorio.");

        if (!model.EsInvitacion && (!model.MontoTotal.HasValue || model.MontoTotal <= 0))
            ModelState.AddModelError("MontoTotal", "El total de la estadía es obligatorio.");

        if (!ModelState.IsValid)
        {
            await PopularViewModel(model);
            return View(model);
        }

        // ── Aplicar cambios ───────────────────────────────────────────────────
        var reserva = await _context.Reservas.FindAsync(model.Id);
        if (reserva == null) return NotFound();

        reserva.NombreHuesped     = model.NombreHuesped;
        reserva.AlojamientoId     = model.AlojamientoId!.Value;
        reserva.CantidadHuespedes = model.CantidadHuespedes!.Value;
        reserva.IncluyeDesayuno   = model.IncluyeDesayuno;
        reserva.FechaIngreso      = model.FechaIngreso!.Value;
        reserva.FechaSalida       = model.FechaSalida!.Value;
        reserva.MontoTotal        = model.MontoTotal ?? 0;
        reserva.EsInvitacion      = model.EsInvitacion;
        reserva.Telefono          = model.Telefono;
        reserva.CanalOrigen       = model.CanalOrigen;
        reserva.Observaciones     = model.Observaciones;
        // FechaCarga no se toca — preserva la fecha original de carga

        await _context.SaveChangesAsync();

        TempData["Mensaje"]     = $"Reserva de {reserva.NombreHuesped} actualizada correctamente.";
        TempData["MensajeTipo"] = "success";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    // ── BuscarTitulares (AJAX) ────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> BuscarTitulares(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(Array.Empty<string>());

        var titulares = await _context.Reservas
            .Where(r => r.NombreHuesped.StartsWith(q))
            .Select(r => r.NombreHuesped)
            .Distinct()
            .OrderBy(n => n)
            .Take(10)
            .ToListAsync();

        return Json(titulares);
    }

    // ── AlojamientosDisponibles (AJAX) ───────────────────────────────────────
    // Devuelve alojamientos sin reservas activas en el rango [fi, fe).
    // La regla de solapamiento es: FechaIngreso < fe AND FechaSalida > fi
    // (checkout del día puede coincidir con check-in nuevo — no se bloquea).

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> AlojamientosDisponibles(
        string fechaIngreso, string fechaEgreso, int? excludeReservaId = null)
    {
        if (!DateTime.TryParse(fechaIngreso, out var fi) ||
            !DateTime.TryParse(fechaEgreso,  out var fe) || fe <= fi)
            return Json(Array.Empty<object>());

        var ocupadosBase = await _context.Reservas
            .Where(r => r.Estado != EstadoReserva.Cancelada
                     && r.FechaIngreso < fe
                     && r.FechaSalida  > fi
                     && (excludeReservaId == null || r.Id != excludeReservaId.Value))
            .Select(r => r.AlojamientoId)
            .Distinct()
            .ToListAsync();

        // Bloqueo bidireccional Apart ↔ Habitaciones componentes
        var apDetalles    = await _context.ApartDetalles.ToListAsync();
        var ocupadosIds   = new HashSet<int>(ocupadosBase);
        foreach (var det in apDetalles)
        {
            if (ocupadosBase.Contains(det.AlojamientoApartId))               // Apart ocupado → bloquear habs
            { ocupadosIds.Add(det.AlojamientoHab1Id); ocupadosIds.Add(det.AlojamientoHab2Id); }
            if (ocupadosBase.Contains(det.AlojamientoHab1Id) ||              // Hab ocupada → bloquear Apart
                ocupadosBase.Contains(det.AlojamientoHab2Id))
            { ocupadosIds.Add(det.AlojamientoApartId); }
        }

        var alojamientos = await _context.Alojamientos
            .Where(a => a.Activo && !ocupadosIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Nombre, a.Capacidad, a.Tipo })
            .ToListAsync();

        var apartDetalles = await _context.ApartDetalles
            .Include(d => d.AlojamientoHab1)
            .Include(d => d.AlojamientoHab2)
            .Where(d => alojamientos.Select(a => a.Id).Contains(d.AlojamientoApartId))
            .ToDictionaryAsync(
                d => d.AlojamientoApartId,
                d => $"Hab. {d.AlojamientoHab1.Nombre.Split(' ').Last()}" +
                     $" y {d.AlojamientoHab2.Nombre.Split(' ').Last()}");

        var result = alojamientos
            .OrderBy(a => a.Tipo == TipoAlojamiento.Habitacion ? 0
                        : a.Tipo == TipoAlojamiento.Apart      ? 1 : 2)
            .ThenBy(a =>
            {
                var p = a.Nombre.Split(' ');
                return p.Length > 1 && int.TryParse(p[^1], out var n) ? n : 0;
            })
            .Select(a => new
            {
                id        = a.Id,
                nombre    = a.Tipo == TipoAlojamiento.Apart &&
                            apartDetalles.TryGetValue(a.Id, out var det)
                                ? $"{a.Nombre} ({det})" : a.Nombre,
                capacidad = a.Capacidad
            })
            .ToList();

        return Json(result);
    }

    // ── ObtenerPrecioSugerido (AJAX) ──────────────────────────────────────────
    // Detecta la temporada según fechaIngreso y devuelve los precios de la tarifa
    // configurada para esa temporada. Fallback a Temporada Baja si no hay match.

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ObtenerPrecioSugerido(
        int alojamientoId, int personas, string? fechaIngreso = null)
    {
        var temporadas = await _context.Temporadas
            .Where(t => t.Activo)
            .ToListAsync();

        // Detectar temporada por fecha de ingreso
        Models.Temporada? temporada = null;
        if (DateTime.TryParse(fechaIngreso, out var fi))
            temporada = temporadas.FirstOrDefault(t => fi >= t.FechaInicio && fi <= t.FechaFin);

        temporada ??= temporadas.FirstOrDefault(t => t.Nombre == "Temporada Baja")
                      ?? temporadas.FirstOrDefault();

        if (temporada == null)
            return Json(new { precioConDesayuno = (decimal?)null, precioSinDesayuno = (decimal?)null });

        var tarifa = await _context.Tarifas.FirstOrDefaultAsync(t =>
            t.AlojamientoId    == alojamientoId &&
            t.CantidadPersonas == personas       &&
            t.TemporadaId      == temporada.Id   &&
            t.Activo);

        return Json(new
        {
            precioConDesayuno = tarifa?.PrecioConDesayuno > 0 ? (decimal?)tarifa.PrecioConDesayuno : null,
            precioSinDesayuno = tarifa?.PrecioSinDesayuno > 0 ? (decimal?)tarifa.PrecioSinDesayuno : null,
            temporada         = temporada.Nombre
        });
    }

    // ── Cancelar ──────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Cancelar(int id)
    {
        var reserva = await _context.Reservas.FindAsync(id);
        if (reserva == null) return NotFound();

        reserva.Estado = EstadoReserva.Cancelada;
        await _context.SaveChangesAsync();

        TempData["Mensaje"]     = $"La reserva de {reserva.NombreHuesped} fue cancelada.";
        TempData["MensajeTipo"] = "warning";
        return RedirectToAction(nameof(Index));
    }

    // ── Crear GET ─────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Crear()
    {
        var vm = new ReservaViewModel { IncluyeDesayuno = true };
        await PopularViewModel(vm);
        return View(vm);
    }

    // ── Crear POST ────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Crear(ReservaViewModel model)
    {
        // TEMPORAL: validación "no anterior a hoy" removida para poder cargar reservas
        // históricas. Restaurar este bloque una vez finalizada la carga histórica.
        // if (model.FechaIngreso.HasValue && model.FechaIngreso.Value.Date < DateTime.Today)
        //     ModelState.AddModelError("FechaIngreso", "La fecha de ingreso no puede ser anterior a hoy.");

        if (model.FechaIngreso.HasValue && model.FechaSalida.HasValue
            && model.FechaSalida.Value <= model.FechaIngreso.Value)
            ModelState.AddModelError("FechaSalida", "La fecha de egreso debe ser posterior a la de ingreso.");

        if (model.AlojamientoId.HasValue && model.CantidadHuespedes.HasValue)
        {
            var alojamiento = await _context.Alojamientos.FindAsync(model.AlojamientoId.Value);
            if (alojamiento != null && model.CantidadHuespedes.Value > alojamiento.Capacidad)
                ModelState.AddModelError("CantidadHuespedes",
                    $"Supera la capacidad máxima de {alojamiento.Capacidad} personas.");
        }

        if (!model.EsInvitacion && (!model.PrecioPorDia.HasValue || model.PrecioPorDia <= 0))
            ModelState.AddModelError("PrecioPorDia", "El precio por día es obligatorio.");

        if (!model.EsInvitacion && (!model.MontoTotal.HasValue || model.MontoTotal <= 0))
            ModelState.AddModelError("MontoTotal", "El total de la estadía es obligatorio.");

        if (!ModelState.IsValid)
        {
            await PopularViewModel(model);
            return View(model);
        }

        var reserva = new Reserva
        {
            NombreHuesped     = model.NombreHuesped,
            AlojamientoId     = model.AlojamientoId!.Value,
            CantidadHuespedes = model.CantidadHuespedes!.Value,
            Telefono          = model.Telefono,
            FechaIngreso      = model.FechaIngreso!.Value,
            FechaSalida       = model.FechaSalida!.Value,
            MontoTotal        = model.MontoTotal ?? 0,
            MontoSena         = 0,
            EsInvitacion      = model.EsInvitacion,
            IncluyeDesayuno   = model.IncluyeDesayuno,
            CanalOrigen       = model.CanalOrigen,
            Observaciones     = model.Observaciones,
            Estado            = EstadoReserva.Confirmada,
            FechaCarga        = DateTime.Now,
        };

        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();

        TempData["Mensaje"]     = $"Reserva de {reserva.NombreHuesped} guardada correctamente.";
        TempData["MensajeTipo"] = "success";
        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task PopularViewModel(ReservaViewModel vm)
    {
        var alojamientos = await _context.Alojamientos
            .Where(a => a.Activo)
            .Select(a => new { a.Id, a.Nombre, a.Capacidad, a.Tipo })
            .ToListAsync();

        var apartDetalles = await _context.ApartDetalles
            .Include(d => d.AlojamientoHab1)
            .Include(d => d.AlojamientoHab2)
            .ToDictionaryAsync(
                d => d.AlojamientoApartId,
                d => $"Hab. {d.AlojamientoHab1.Nombre.Split(' ').Last()} y {d.AlojamientoHab2.Nombre.Split(' ').Last()}"
            );

        var ordenados = alojamientos
            .OrderBy(a => a.Tipo == TipoAlojamiento.Habitacion ? 0
                        : a.Tipo == TipoAlojamiento.Apart      ? 1 : 2)
            .ThenBy(a =>
            {
                var partes = a.Nombre.Split(' ');
                return partes.Length > 1 && int.TryParse(partes[^1], out var n) ? n : 0;
            })
            .ToList();

        vm.Alojamientos = ordenados.Select(a => new SelectListItem
        {
            Value = a.Id.ToString(),
            Text  = a.Tipo == TipoAlojamiento.Apart && apartDetalles.TryGetValue(a.Id, out var detalle)
                        ? $"{a.Nombre} ({detalle})"
                        : a.Nombre
        }).ToList();

        ViewBag.AlojamientosJson = System.Text.Json.JsonSerializer.Serialize(
            ordenados.Select(a => new { id = a.Id, capacidad = a.Capacidad }));
    }
}
