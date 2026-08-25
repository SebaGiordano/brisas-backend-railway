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
        string? ordenar,
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

        // ── Filtro por nombre (expandido a Reservas Vinculadas) ───────────────
        // Si el texto buscado coincide con el nombre de una reserva, o con la
        // etiqueta de un grupo vinculado, se incluyen también todas las demás
        // reservas de ese mismo grupo — aunque su nombre no coincida.
        Dictionary<int, string> vinculacionPorReservaId = new();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var coincidenNombre = await _context.Reservas
                .Where(r => r.NombreHuesped.Contains(busqueda))
                .Select(r => new { r.Id, r.NombreHuesped, r.GrupoVinculadoId })
                .ToListAsync();

            var idsCoincidenNombre = coincidenNombre.Select(x => x.Id).ToHashSet();

            // Nombre REAL (no el texto buscado) de las reservas que matchearon
            // directamente por nombre, agrupado por GrupoVinculadoId — evita
            // etiquetas ambiguas como "Vinculada con: L" cuando la búsqueda es corta.
            var nombresDirectosPorGrupo = coincidenNombre
                .Where(x => x.GrupoVinculadoId.HasValue)
                .GroupBy(x => x.GrupoVinculadoId!.Value)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.NombreHuesped).Distinct()));

            var gruposCoincidenEtiqueta = await _context.GruposVinculados
                .Where(g => g.Etiqueta != null && g.Etiqueta.Contains(busqueda))
                .Select(g => new { g.Id, g.Etiqueta })
                .ToListAsync();
            var etiquetaPorGrupo = gruposCoincidenEtiqueta.ToDictionary(g => g.Id, g => g.Etiqueta!);

            var todosLosGrupos = etiquetaPorGrupo.Keys.Union(nombresDirectosPorGrupo.Keys).Distinct().ToList();

            var idsFinal = idsCoincidenNombre;

            if (todosLosGrupos.Any())
            {
                var reservasDeGrupos = await _context.Reservas
                    .Where(r => r.GrupoVinculadoId.HasValue && todosLosGrupos.Contains(r.GrupoVinculadoId.Value))
                    .Select(r => new { r.Id, r.NombreHuesped, r.GrupoVinculadoId })
                    .ToListAsync();

                foreach (var r in reservasDeGrupos)
                {
                    if (!idsFinal.Contains(r.Id))
                    {
                        idsFinal.Add(r.Id);
                        // Prioridad: 1) nombre real de quien matcheó en su grupo,
                        // 2) etiqueta del grupo si matcheó por ahí, 3) texto buscado
                        // (solo si ninguna de las dos anteriores aplica).
                        vinculacionPorReservaId[r.Id] =
                            nombresDirectosPorGrupo.TryGetValue(r.GrupoVinculadoId!.Value, out var nombresReales) ? nombresReales
                            : etiquetaPorGrupo.TryGetValue(r.GrupoVinculadoId!.Value, out var etiq) ? etiq
                            : busqueda;
                    }
                }
            }

            query = query.Where(r => idsFinal.Contains(r.Id));
        }

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

        var ordenado = (ordenar == "antiguas")
            ? query.OrderBy(r => r.FechaIngreso)
            : query.OrderByDescending(r => r.FechaIngreso);

        var reservas = await ordenado
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
                TotalCobrado         = r.Pagos.Sum(p => p.Monto),
                Estado               = r.Estado,
                EsGrupal             = r.EsGrupal,
                CantUnidadesGrupales = r.UnidadesGrupales.Count()
            })
            .ToListAsync();

        foreach (var item in reservas)
            if (vinculacionPorReservaId.TryGetValue(item.Id, out var motivo))
                item.VinculadaConNombre = motivo;

        ViewBag.Busqueda     = busqueda    ?? string.Empty;
        ViewBag.Estado       = estado;
        ViewBag.EstadoPago   = estadoPago  ?? string.Empty;
        ViewBag.Desde        = desde?.ToString("yyyy-MM-dd") ?? string.Empty;
        ViewBag.Hasta        = hasta?.ToString("yyyy-MM-dd") ?? string.Empty;
        ViewBag.Ordenar      = (ordenar == "antiguas") ? "antiguas" : "recientes";
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
            .Include(r => r.UnidadesGrupales)
                .ThenInclude(u => u.Alojamiento)
            .Include(r => r.GrupoVinculado)
                .ThenInclude(g => g!.Reservas)
                    .ThenInclude(r => r.Alojamiento)
            .Include(r => r.GrupoVinculado)
                .ThenInclude(g => g!.Reservas)
                    .ThenInclude(r => r.UnidadesGrupales)
            .Include(r => r.GrupoVinculado)
                .ThenInclude(g => g!.Reservas)
                    .ThenInclude(r => r.Pagos)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva == null) return NotFound();

        var apartIds = reserva.UnidadesGrupales
            .Where(u => u.Alojamiento.Tipo == TipoAlojamiento.Apart)
            .Select(u => u.AlojamientoId)
            .ToList();

        if (apartIds.Any())
        {
            var detalles = await _context.ApartDetalles
                .Include(d => d.AlojamientoHab1)
                .Include(d => d.AlojamientoHab2)
                .Where(d => apartIds.Contains(d.AlojamientoApartId))
                .ToListAsync();

            ViewBag.ApartComposicion = detalles.ToDictionary(
                d => d.AlojamientoApartId,
                d => $"{d.AlojamientoHab1.Nombre.Replace("Habitación", "Hab.")} y {d.AlojamientoHab2.Nombre.Replace("Habitación", "Hab.")}"
            );
        }

        if (reserva.PrecioAireDiario.HasValue)
        {
            var diasPorCabana = await _context.ReservaAireDias
                .Where(a => a.ReservaId == id)
                .GroupBy(a => a.AlojamientoId)
                .Select(g => new { alojamientoId = g.Key, cantidad = g.Count() })
                .ToListAsync();

            ViewBag.DiasAirePorCabana = diasPorCabana.ToDictionary(d => d.alojamientoId, d => d.cantidad);
        }

        if (reserva.GrupoVinculado != null)
        {
            ViewBag.ReservasVinculadas = reserva.GrupoVinculado.Reservas
                .Where(r => r.Id != reserva.Id)
                .Select(r => new
                {
                    id = r.Id,
                    nombreHuesped = r.NombreHuesped,
                    esGrupal = r.EsGrupal,
                    cantUnidades = r.UnidadesGrupales.Count,
                    nombreAlojamiento = r.Alojamiento.Nombre,
                    saldoPendiente = r.MontoTotal - r.Pagos.Sum(p => p.Monto)
                })
                .ToList();
        }

        return View(reserva);
    }

    // ── Editar GET ────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar(int id)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Alojamiento)
            .Include(r => r.UnidadesGrupales)
                .ThenInclude(u => u.Alojamiento)
            .Include(r => r.GrupoVinculado)
                .ThenInclude(g => g!.Reservas)
                    .ThenInclude(r => r.Alojamiento)
            .Include(r => r.GrupoVinculado)
                .ThenInclude(g => g!.Reservas)
                    .ThenInclude(r => r.UnidadesGrupales)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva == null) return NotFound();

        int noches = (reserva.FechaSalida - reserva.FechaIngreso).Days;

        // El aire ya guardado se descuenta del MontoTotal real para obtener el
        // subtotal puro de alojamiento — el formulario edita ese valor puro,
        // nunca el total final con aire incluido.
        decimal subtotalAireGuardado = 0;
        if (reserva.PrecioAireDiario.HasValue)
        {
            int diasAireGuardados = await _context.ReservaAireDias
                .CountAsync(a => a.ReservaId == id);
            subtotalAireGuardado = diasAireGuardados * reserva.PrecioAireDiario.Value;
        }
        decimal montoAlojamiento = reserva.MontoTotal - subtotalAireGuardado;

        decimal precioPorDia = (noches > 0 && !reserva.EsInvitacion)
                                ? Math.Round(montoAlojamiento / noches, 2)
                                : 0;

        var vm = new EditarReservaViewModel
        {
            Id                     = reserva.Id,
            NombreHuesped          = reserva.NombreHuesped,
            AlojamientoId          = reserva.AlojamientoId,
            CantidadHuespedes      = reserva.CantidadHuespedes,
            IncluyeDesayuno        = reserva.IncluyeDesayuno,
            FechaIngreso           = reserva.FechaIngreso,
            FechaSalida            = reserva.FechaSalida,
            PrecioPorDia           = precioPorDia,
            MontoTotal             = montoAlojamiento,
            UsaAireAcondicionado   = reserva.PrecioAireDiario.HasValue,
            PrecioAireDiario       = reserva.PrecioAireDiario,
            EsInvitacion      = reserva.EsInvitacion,
            Telefono          = reserva.Telefono,
            CanalOrigen       = reserva.CanalOrigen,
            Observaciones     = reserva.Observaciones,
            EsGrupal          = reserva.EsGrupal,
            UnidadesGrupales  = reserva.UnidadesGrupales
                .Select(u => new UnidadGrupalViewModel
                {
                    AlojamientoId     = u.AlojamientoId,
                    CantidadHuespedes = u.CantidadHuespedes
                }).ToList(),
            EtiquetaGrupoVinculado = reserva.GrupoVinculado?.Etiqueta,
        };

        ViewBag.UnidadesGrupalesJson = System.Text.Json.JsonSerializer.Serialize(
            reserva.UnidadesGrupales.Select(u => new
            {
                id        = u.AlojamientoId,
                nombre    = u.Alojamiento.Nombre,
                capacidad = u.Alojamiento.Capacidad,
                personas  = u.CantidadHuespedes,
                tipo      = u.Alojamiento.Tipo.ToString()
            }));

        var aireDiasGuardados = await _context.ReservaAireDias
            .Where(a => a.ReservaId == id)
            .Select(a => new { alojamientoId = a.AlojamientoId, fecha = a.Fecha.ToString("yyyy-MM-dd") })
            .ToListAsync();
        ViewBag.AireDiasJson = System.Text.Json.JsonSerializer.Serialize(aireDiasGuardados);

        if (reserva.GrupoVinculado != null)
        {
            var vinculadas = reserva.GrupoVinculado.Reservas
                .Where(r => r.Id != reserva.Id && r.Estado != EstadoReserva.Cancelada)
                .Select(r => new
                {
                    id = r.Id,
                    nombreHuesped = r.NombreHuesped,
                    esGrupal = r.EsGrupal,
                    cantUnidades = r.UnidadesGrupales.Count,
                    nombreAlojamiento = r.Alojamiento.Nombre
                })
                .ToList();

            ViewBag.ReservasVinculadasJson = System.Text.Json.JsonSerializer.Serialize(vinculadas);
        }

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

        if (model.EsGrupal)
        {
            if (model.UnidadesGrupales.Count < 2)
                ModelState.AddModelError("", "Una reserva grupal debe incluir al menos 2 unidades.");
            if (model.UnidadesGrupales.Any(u => u.CantidadHuespedes < 1))
                ModelState.AddModelError("", "Cada unidad debe tener al menos 1 persona asignada.");
        }
        else
        {
            if (model.AlojamientoId.HasValue && model.CantidadHuespedes.HasValue)
            {
                var aloj = await _context.Alojamientos.FindAsync(model.AlojamientoId.Value);
                if (aloj != null && model.CantidadHuespedes.Value > aloj.Capacidad)
                    ModelState.AddModelError("CantidadHuespedes",
                        $"Supera la capacidad máxima de {aloj.Capacidad} personas.");
            }

            if (!model.EsInvitacion && (!model.PrecioPorDia.HasValue || model.PrecioPorDia <= 0))
                ModelState.AddModelError("PrecioPorDia", "El precio por día es obligatorio.");
        }

        if (!model.EsInvitacion && (!model.MontoTotal.HasValue || model.MontoTotal <= 0))
            ModelState.AddModelError("MontoTotal", "El total de la estadía es obligatorio.");

        if (!ModelState.IsValid)
        {
            await PopularViewModel(model);
            if (model.EsGrupal && model.UnidadesGrupales.Any())
            {
                var alojIds = model.UnidadesGrupales.Select(u => u.AlojamientoId).ToList();
                var alojs   = await _context.Alojamientos
                    .Where(a => alojIds.Contains(a.Id))
                    .Select(a => new { a.Id, a.Nombre, a.Capacidad, a.Tipo })
                    .ToListAsync();
                var alojMap = alojs.ToDictionary(a => a.Id);
                ViewBag.UnidadesGrupalesJson = System.Text.Json.JsonSerializer.Serialize(
                    model.UnidadesGrupales.Select(u =>
                    {
                        alojMap.TryGetValue(u.AlojamientoId, out var a);
                        return new
                        {
                            id        = u.AlojamientoId,
                            nombre    = a?.Nombre    ?? string.Empty,
                            capacidad = a?.Capacidad ?? 0,
                            personas  = u.CantidadHuespedes,
                            tipo      = a?.Tipo.ToString() ?? string.Empty
                        };
                    }));
            }
            else
            {
                ViewBag.UnidadesGrupalesJson = "[]";
            }
            return View(model);
        }

        // ── Aplicar cambios ───────────────────────────────────────────────────
        var reserva = await _context.Reservas.FindAsync(model.Id);
        if (reserva == null) return NotFound();

        reserva.NombreHuesped     = model.NombreHuesped;
        reserva.EsGrupal          = model.EsGrupal;
        reserva.IncluyeDesayuno   = model.IncluyeDesayuno;
        reserva.FechaIngreso      = model.FechaIngreso!.Value;
        reserva.FechaSalida       = model.FechaSalida!.Value;
        reserva.MontoTotal        = model.MontoTotal ?? 0;
        reserva.EsInvitacion      = model.EsInvitacion;
        reserva.Telefono          = model.Telefono;
        reserva.CanalOrigen       = model.CanalOrigen;
        reserva.Observaciones     = model.Observaciones;
        // FechaCarga no se toca — preserva la fecha original de carga

        if (model.EsGrupal && model.UnidadesGrupales.Count > 0)
        {
            reserva.AlojamientoId     = model.UnidadesGrupales[0].AlojamientoId;
            reserva.CantidadHuespedes = model.UnidadesGrupales.Sum(u => u.CantidadHuespedes);
        }
        else
        {
            reserva.AlojamientoId     = model.AlojamientoId!.Value;
            reserva.CantidadHuespedes = model.CantidadHuespedes!.Value;
        }

        // Sync ReservaAlojamientos
        var existentes = await _context.ReservaAlojamientos
            .Where(ra => ra.ReservaId == reserva.Id)
            .ToListAsync();
        _context.ReservaAlojamientos.RemoveRange(existentes);

        if (model.EsGrupal)
        {
            foreach (var u in model.UnidadesGrupales)
                _context.ReservaAlojamientos.Add(new ReservaAlojamiento
                {
                    ReservaId         = reserva.Id,
                    AlojamientoId     = u.AlojamientoId,
                    CantidadHuespedes = u.CantidadHuespedes
                });
        }

        await SincronizarAireAcondicionado(reserva, model);
        await SincronizarVinculacion(reserva, model);

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
            .Where(r => r.NombreHuesped.ToLower().StartsWith(q.ToLower()))
            .Select(r => r.NombreHuesped)
            .Distinct()
            .OrderBy(n => n)
            .Take(10)
            .ToListAsync();

        return Json(titulares);
    }

    // ── BuscarReservasParaVincular (AJAX) ───────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> BuscarReservasParaVincular(string q, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(Array.Empty<object>());

        var query = _context.Reservas
            .Include(r => r.Alojamiento)
            .Include(r => r.UnidadesGrupales)
            .Where(r => r.Estado != EstadoReserva.Cancelada
                     && r.NombreHuesped.ToLower().Contains(q.ToLower()));

        if (excludeId.HasValue)
            query = query.Where(r => r.Id != excludeId.Value);

        var resultados = await query
            .OrderBy(r => r.NombreHuesped)
            .Take(10)
            .Select(r => new
            {
                id = r.Id,
                nombreHuesped = r.NombreHuesped,
                esGrupal = r.EsGrupal,
                cantUnidades = r.UnidadesGrupales.Count,
                nombreAlojamiento = r.Alojamiento.Nombre,
                fechaIngreso = r.FechaIngreso.ToString("dd/MM/yyyy"),
                fechaSalida = r.FechaSalida.ToString("dd/MM/yyyy")
            })
            .ToListAsync();

        return Json(resultados);
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

        var reservasEnRango = await _context.Reservas
            .Where(r => r.Estado != EstadoReserva.Cancelada
                     && r.FechaIngreso < fe
                     && r.FechaSalida  > fi
                     && (excludeReservaId == null || r.Id != excludeReservaId.Value))
            .Select(r => new { r.Id, r.AlojamientoId, r.EsGrupal })
            .ToListAsync();

        var reservaIdsEnRango = reservasEnRango.Select(r => r.Id).ToList();

        // Grupales: las unidades reales están en ReservaAlojamientos, no en AlojamientoId
        var ocupadosGrupales = await _context.ReservaAlojamientos
            .Where(ra => reservaIdsEnRango.Contains(ra.ReservaId))
            .Select(ra => ra.AlojamientoId)
            .ToListAsync();

        var ocupadosBase = reservasEnRango
            .Where(r => !r.EsGrupal)
            .Select(r => r.AlojamientoId)
            .Concat(ocupadosGrupales)
            .Distinct()
            .ToList();

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
                capacidad = a.Capacidad,
                tipo      = a.Tipo.ToString()
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

    // ── Eliminar ──────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Pagos)
            .Include(r => r.UnidadesGrupales)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva == null) return NotFound();
        if (reserva.Estado != EstadoReserva.Cancelada)
            return BadRequest("Solo se pueden eliminar reservas canceladas.");

        _context.Pagos.RemoveRange(reserva.Pagos);
        _context.ReservaAlojamientos.RemoveRange(reserva.UnidadesGrupales);
        _context.Reservas.Remove(reserva);
        await _context.SaveChangesAsync();

        TempData["Mensaje"]     = $"La reserva de {reserva.NombreHuesped} fue eliminada permanentemente.";
        TempData["MensajeTipo"] = "success";
        return RedirectToAction(nameof(Index), new { estado = "Cancelada" });
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

        if (!model.EsGrupal && model.AlojamientoId.HasValue && model.CantidadHuespedes.HasValue)
        {
            var alojamiento = await _context.Alojamientos.FindAsync(model.AlojamientoId.Value);
            if (alojamiento != null && model.CantidadHuespedes.Value > alojamiento.Capacidad)
                ModelState.AddModelError("CantidadHuespedes",
                    $"Supera la capacidad máxima de {alojamiento.Capacidad} personas.");
        }

        if (model.EsGrupal)
        {
            if (model.UnidadesGrupales.Count < 2)
                ModelState.AddModelError("", "Una reserva grupal debe incluir al menos 2 unidades.");
            if (model.UnidadesGrupales.Any(u => u.CantidadHuespedes < 1))
                ModelState.AddModelError("", "Cada unidad debe tener al menos 1 persona asignada.");
        }

        if (!model.EsInvitacion && (!model.PrecioPorDia.HasValue || model.PrecioPorDia <= 0)
            && !model.EsGrupal)
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
            EsGrupal          = model.EsGrupal,
            CanalOrigen       = model.CanalOrigen,
            Observaciones     = model.Observaciones,
            Estado            = EstadoReserva.Confirmada,
            FechaCarga        = DateTime.Now,
        };

        if (model.EsGrupal)
        {
            foreach (var u in model.UnidadesGrupales)
                reserva.UnidadesGrupales.Add(new ReservaAlojamiento
                {
                    AlojamientoId     = u.AlojamientoId,
                    CantidadHuespedes = u.CantidadHuespedes
                });
        }

        _context.Reservas.Add(reserva);
        await SincronizarAireAcondicionado(reserva, model);
        await SincronizarVinculacion(reserva, model);
        await _context.SaveChangesAsync();

        TempData["Mensaje"]     = $"Reserva de {reserva.NombreHuesped} guardada correctamente.";
        TempData["MensajeTipo"] = "success";
        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Reemplaza por completo las filas de ReservaAireDias de la reserva según lo
    // que vino del formulario: borra las existentes (si las hubiera) y crea una
    // fila por cada día tildado dentro del rango vigente de la reserva (fechas
    // fuera de [FechaIngreso, FechaSalida) se ignoran como respaldo, aunque el
    // frontend no debería permitir tildarlas).
    private async Task SincronizarAireAcondicionado(Reserva reserva, ReservaViewModel model)
    {
        if (reserva.Id > 0)
        {
            var existentes = await _context.ReservaAireDias
                .Where(a => a.ReservaId == reserva.Id)
                .ToListAsync();
            _context.ReservaAireDias.RemoveRange(existentes);
        }

        var totalFechas = model.AireCabanas.Sum(c => c.Fechas.Count);
        if (!model.UsaAireAcondicionado || totalFechas == 0)
        {
            reserva.PrecioAireDiario = null;
            return;
        }

        reserva.PrecioAireDiario = model.PrecioAireDiario;

        var primerNoche = reserva.FechaIngreso.Date;
        var ultimaNoche = reserva.FechaSalida.Date.AddDays(-1);

        foreach (var cabana in model.AireCabanas)
        {
            foreach (var fecha in cabana.Fechas.Select(f => f.Date).Distinct())
            {
                if (fecha < primerNoche || fecha > ultimaNoche) continue;

                _context.ReservaAireDias.Add(new ReservaAireDia
                {
                    Reserva       = reserva,
                    AlojamientoId = cabana.AlojamientoId,
                    Fecha         = fecha
                });
            }
        }
    }

    private async Task SincronizarVinculacion(Reserva reserva, ReservaViewModel model)
    {
        // Caso 1: se pidió eliminar el vínculo explícitamente.
        if (model.EliminarVinculo)
        {
            var grupoAnteriorId = reserva.GrupoVinculadoId;
            reserva.GrupoVinculadoId = null;
            await LimpiarGrupoSiQuedaSolo(grupoAnteriorId);
            return;
        }

        // Caso 2: no se seleccionó ninguna reserva para vincular — no tocar nada
        // del vínculo existente (puede que ya tuviera uno de antes).
        if (!model.ReservasVinculadasIds.Any())
            return;

        // Reunir todas las reservas involucradas: la actual + las elegidas del buscador.
        var idsInvolucrados = model.ReservasVinculadasIds.Append(reserva.Id).Distinct().ToList();
        var reservasInvolucradas = await _context.Reservas
            .Where(r => idsInvolucrados.Contains(r.Id))
            .ToListAsync();

        // Si reserva.Id todavía es 0 (Crear, antes de SaveChanges), no va a aparecer en
        // la consulta de arriba — se agrega aparte más abajo mediante la navegación.
        var idsExistentesConGrupo = reservasInvolucradas
            .Where(r => r.GrupoVinculadoId.HasValue)
            .Select(r => r.GrupoVinculadoId!.Value)
            .Distinct()
            .ToList();

        GrupoVinculado grupo;

        if (idsExistentesConGrupo.Count == 1)
        {
            // Ya hay un grupo entre las reservas seleccionadas: se reutiliza (permite
            // ir agregando reservas a una cadena existente).
            grupo = await _context.GruposVinculados.FirstAsync(g => g.Id == idsExistentesConGrupo[0]);
        }
        else if (idsExistentesConGrupo.Count > 1)
        {
            // Caso borde: se intentó vincular reservas que ya pertenecen a DOS grupos
            // distintos entre sí. Fusionamos: todas las reservas de ambos grupos pasan
            // al primero, y el segundo grupo (que queda vacío) se elimina.
            grupo = await _context.GruposVinculados.FirstAsync(g => g.Id == idsExistentesConGrupo[0]);
            var otrosGrupos = await _context.GruposVinculados
                .Where(g => idsExistentesConGrupo.Skip(1).Contains(g.Id))
                .ToListAsync();
            var otrasReservas = await _context.Reservas
                .Where(r => otrosGrupos.Select(g => g.Id).Contains(r.GrupoVinculadoId!.Value))
                .ToListAsync();
            foreach (var r in otrasReservas) r.GrupoVinculado = grupo;
            _context.GruposVinculados.RemoveRange(otrosGrupos);
        }
        else
        {
            // Ninguna de las reservas seleccionadas tenía grupo todavía: se crea uno nuevo.
            grupo = new GrupoVinculado();
            _context.GruposVinculados.Add(grupo);
        }

        if (!string.IsNullOrWhiteSpace(model.EtiquetaGrupoVinculado))
            grupo.Etiqueta = model.EtiquetaGrupoVinculado;

        // Se asigna la navegación (no el Id) — si "grupo" es nuevo y todavía no tiene
        // Id real (recién se asigna en SaveChangesAsync), EF Core propaga el Id
        // correcto a todas las reservas que apunten a este mismo objeto, gracias al
        // mecanismo de "fixup" de relaciones. Asignar r.GrupoVinculadoId = grupo.Id
        // a mano en este punto sería incorrecto: grupo.Id todavía vale 0.
        foreach (var r in reservasInvolucradas)
            r.GrupoVinculado = grupo;

        reserva.GrupoVinculado = grupo;
    }

    // Si tras quitar una reserva el grupo queda con 0 o 1 reservas, ya no tiene sentido
    // como vínculo — se limpia automáticamente (según lo documentado: "un vínculo de 1
    // no tiene sentido").
    private async Task LimpiarGrupoSiQuedaSolo(int? grupoVinculadoId)
    {
        if (!grupoVinculadoId.HasValue) return;
        var cantidadRestante = await _context.Reservas.CountAsync(r => r.GrupoVinculadoId == grupoVinculadoId.Value);
        if (cantidadRestante <= 1)
        {
            var reservaSolitaria = await _context.Reservas.FirstOrDefaultAsync(r => r.GrupoVinculadoId == grupoVinculadoId.Value);
            if (reservaSolitaria != null) reservaSolitaria.GrupoVinculadoId = null;
            var grupo = await _context.GruposVinculados.FindAsync(grupoVinculadoId.Value);
            if (grupo != null) _context.GruposVinculados.Remove(grupo);
        }
    }

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
