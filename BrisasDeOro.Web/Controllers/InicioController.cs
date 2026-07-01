using System.Text.RegularExpressions;
using BrisasDeOro.Web.Data;
using BrisasDeOro.Web.Models;
using BrisasDeOro.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrisasDeOro.Web.Controllers;

[Authorize(Roles = "Administrador,Viewer")]
public class InicioController : Controller
{
    private readonly ApplicationDbContext _context;

    public InicioController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var hoy    = DateOnly.FromDateTime(DateTime.Today);
        var manana = hoy.AddDays(1);

        // Una sola consulta cubre todos los datos necesarios:
        // activas hoy, check-ins/outs hoy y mañana, pendientes 24 hs.
        var reservas = await _context.Reservas
            .Where(r => r.Estado != EstadoReserva.Cancelada
                     && r.FechaIngreso < manana.AddDays(1).ToDateTime(TimeOnly.MinValue)
                     && r.FechaSalida  >= hoy.ToDateTime(TimeOnly.MinValue))
            .Include(r => r.Alojamiento)
            .Include(r => r.Pagos)
            .Include(r => r.UnidadesGrupales)
                .ThenInclude(u => u.Alojamiento)
            .ToListAsync();

        var alojamientos = (await _context.Alojamientos
            .Where(a => a.Activo)
            .ToListAsync())
            .OrderBy(a => a.Tipo == TipoAlojamiento.Habitacion ? 0
                        : a.Tipo == TipoAlojamiento.Apart      ? 1 : 2)
            .ThenBy(a => NumeroEnNombre(a.Nombre))
            .ThenBy(a => a.Nombre)
            .ToList();

        // ── Apart → display con habitaciones ─────────────────────────────────
        var apartDetalles = await _context.ApartDetalles.ToListAsync();
        var alojNombreMap = alojamientos.ToDictionary(a => a.Id, a => a.Nombre);
        var apartHabsStr  = apartDetalles.ToDictionary(
            det => det.AlojamientoApartId,
            det =>
            {
                var n1 = alojNombreMap.TryGetValue(det.AlojamientoHab1Id, out var s1) ? NumeroEnNombre(s1).ToString() : "?";
                var n2 = alojNombreMap.TryGetValue(det.AlojamientoHab2Id, out var s2) ? NumeroEnNombre(s2).ToString() : "?";
                return $"Hab. {n1} / {n2}";
            });

        // ── Helpers locales ───────────────────────────────────────────────────
        DateOnly FI(Reserva r) => DateOnly.FromDateTime(r.FechaIngreso);
        DateOnly FS(Reserva r) => DateOnly.FromDateTime(r.FechaSalida);

        string NombreAloj(Alojamiento a) =>
            a.Tipo == TipoAlojamiento.Apart && apartHabsStr.TryGetValue(a.Id, out var habs)
                ? $"{a.Nombre} ({habs})"
                : a.Nombre;

        // Una reserva grupal "posee" todas las unidades de UnidadesGrupales, no solo
        // la representativa (AlojamientoId). Esta es la fuente de verdad para saber
        // si un alojamiento está vinculado a una reserva determinada.
        bool PerteneceAReserva(Reserva r, int alojamientoId) =>
            r.EsGrupal && r.UnidadesGrupales.Count > 0
                ? r.UnidadesGrupales.Any(u => u.AlojamientoId == alojamientoId)
                : r.AlojamientoId == alojamientoId;

        // ── Check-ins / outs ──────────────────────────────────────────────────
        // Reserva grupal: se listan todas las unidades del grupo (ReservaAlojamientos),
        // no solo la unidad representativa.
        MovimientoItem CrearMovimiento(Reserva r) =>
            r.EsGrupal && r.UnidadesGrupales.Count > 0
                ? new MovimientoItem
                  {
                      ReservaId = r.Id,
                      NombreHuesped = r.NombreHuesped,
                      EsGrupal = true,
                      NombresUnidadesGrupales = r.UnidadesGrupales
                          .Select(u => NombreAloj(u.Alojamiento))
                          .ToList()
                  }
                : new MovimientoItem
                  {
                      ReservaId = r.Id, NombreHuesped = r.NombreHuesped,
                      NombreAlojamiento = NombreAloj(r.Alojamiento)
                  };

        var checkInsHoy      = reservas.Where(r => FI(r) == hoy).Select(CrearMovimiento).ToList();
        var checkOutsHoy     = reservas.Where(r => FS(r) == hoy).Select(CrearMovimiento).ToList();
        var checkInsManana   = reservas.Where(r => FI(r) == manana).Select(CrearMovimiento).ToList();
        var checkOutsManana  = reservas.Where(r => FS(r) == manana).Select(CrearMovimiento).ToList();

        // ── Activas hoy (FI <= hoy < FS) ─────────────────────────────────────
        var activasHoy = reservas.Where(r => FI(r) <= hoy && FS(r) > hoy).ToList();

        // ── Plazas ocupadas hoy (sin invitaciones) ────────────────────────────
        var plazas = activasHoy.Where(r => !r.EsInvitacion).Sum(r => r.CantidadHuespedes);

        // ── Comensales desayuno hoy ───────────────────────────────────────────
        // FI < hoy: excluye quien ingresa hoy (no desayuna el día de llegada)
        // FS >= hoy: incluye quien hace checkout hoy (desayuna antes de irse)
        var comensalesHoy = reservas
            .Where(r => FI(r) < hoy && FS(r) >= hoy && r.IncluyeDesayuno)
            .Sum(r => r.CantidadHuespedes);

        // ── Comensales desayuno mañana ────────────────────────────────────────
        // FI < mañana: excluye quien ingresa ese día (no desayuna el día de llegada)
        // FS >= mañana: incluye quien hace checkout ese día (desayuna antes de irse)
        var comensales = reservas
            .Where(r => FI(r) < manana && FS(r) >= manana && r.IncluyeDesayuno)
            .Sum(r => r.CantidadHuespedes);

        // ── Unidades libres / ocupadas hoy ────────────────────────────────────
        // Si un Apart está ocupado, sus habitaciones componentes se marcan como ocupadas.
        // Reserva grupal: TODAS sus unidades (no solo la representativa) cuentan como
        // ocupadas.
        var ocupadosHoyIds = activasHoy
            .SelectMany(r => r.EsGrupal && r.UnidadesGrupales.Count > 0
                ? r.UnidadesGrupales.Select(u => u.AlojamientoId)
                : new[] { r.AlojamientoId })
            .ToHashSet();

        var ocupadosExpandidos = new HashSet<int>(ocupadosHoyIds);
        foreach (var det in apartDetalles)
        {
            if (ocupadosHoyIds.Contains(det.AlojamientoApartId))           // Apart ocupado → habs ocupadas
            { ocupadosExpandidos.Add(det.AlojamientoHab1Id); ocupadosExpandidos.Add(det.AlojamientoHab2Id); }
            if (ocupadosHoyIds.Contains(det.AlojamientoHab1Id) ||          // Hab ocupada → Apart bloqueado
                ocupadosHoyIds.Contains(det.AlojamientoHab2Id))
            { ocupadosExpandidos.Add(det.AlojamientoApartId); }
        }

        var unidadesLibres   = alojamientos
            .Where(a => !ocupadosExpandidos.Contains(a.Id) && a.Tipo != TipoAlojamiento.Apart)
            .Select(a => a.Nombre)
            .ToList();
        var unidadesOcupadas = alojamientos
            .Where(a => ocupadosExpandidos.Contains(a.Id) && a.Tipo != TipoAlojamiento.Apart)
            .Select(a => a.Nombre)
            .ToList();

        // Mapa habId → reserva activa del Apart que la incluye (para limpieza diaria
        // cuando el Apart está reservado y sus habs individuales no tienen reserva propia).
        var habToApartActiva = new Dictionary<int, Reserva>();
        foreach (var det in apartDetalles)
        {
            var apartActiva = activasHoy.FirstOrDefault(r =>
                r.AlojamientoId == det.AlojamientoApartId ||
                (r.EsGrupal && r.UnidadesGrupales.Any(u => u.AlojamientoId == det.AlojamientoApartId)));
            if (apartActiva != null)
            {
                habToApartActiva[det.AlojamientoHab1Id] = apartActiva;
                habToApartActiva[det.AlojamientoHab2Id] = apartActiva;
            }
        }

        // ── Limpieza del día ──────────────────────────────────────────────────
        // Primera pasada: tareas por unidad individual, detectando además a qué
        // reserva grupal (si la hay) pertenece esa unidad.
        var tareasPorUnidad = new List<(Alojamiento Aloj, List<string> Tareas, Reserva? ReservaGrupal)>();

        foreach (var aloj in alojamientos)
        {
            var tareas = new List<string>();

            var checkOut = reservas.FirstOrDefault(r => PerteneceAReserva(r, aloj.Id) && FS(r) == hoy);
            var checkIn  = reservas.FirstOrDefault(r => PerteneceAReserva(r, aloj.Id) && FI(r) == hoy);
            var activa   = activasHoy.FirstOrDefault(r => PerteneceAReserva(r, aloj.Id));

            if (checkOut != null)
                tareas.Add("Limpieza profunda + cambio total de blancos (ropa de cama y baño) — Checkout");

            if (checkIn != null && checkOut == null)
                tareas.Add("Preparación para ingreso (check-in)");

            if (activa != null)
            {
                var dias = (hoy.ToDateTime(TimeOnly.MinValue) - activa.FechaIngreso.Date).Days;

                if (dias == 6)
                    tareas.Add("Cambio de sábanas (día 6)");

                if (aloj.Tipo == TipoAlojamiento.Habitacion && checkOut == null && checkIn == null)
                    tareas.Add("Limpieza regular diaria");

                if (aloj.Tipo == TipoAlojamiento.Cabaña && dias > 0 && (dias + 1) % 3 == 0)
                    tareas.Add($"Limpieza regular (día {dias + 1})");
            }
            else if (aloj.Tipo == TipoAlojamiento.Habitacion
                     && checkOut == null
                     && habToApartActiva.ContainsKey(aloj.Id))
            {
                // Habitación componente de un Apart con reserva activa hoy
                tareas.Add("Limpieza regular diaria");
            }

            if (tareas.Count > 0)
            {
                // Misma prioridad que la redacción de tareas: checkout > checkin > activa.
                var reservaGrupal = checkOut?.EsGrupal == true ? checkOut
                                   : checkIn?.EsGrupal  == true ? checkIn
                                   : activa?.EsGrupal   == true ? activa
                                   : habToApartActiva.TryGetValue(aloj.Id, out var apartRes) && apartRes.EsGrupal ? apartRes
                                   : null;
                tareasPorUnidad.Add((aloj, tareas, reservaGrupal));
            }
        }

        // Segunda pasada: agrupar las unidades que pertenecen a una misma reserva
        // grupal en un único LimpiezaItem; el resto queda como unidad individual.
        var limpiezaItems = new List<LimpiezaItem>();
        var unidadesAgrupadas = new HashSet<int>();

        foreach (var (aloj, tareas, reservaGrupal) in tareasPorUnidad)
        {
            if (unidadesAgrupadas.Contains(aloj.Id)) continue;

            if (reservaGrupal != null)
            {
                var unidadesDelGrupo = tareasPorUnidad
                    .Where(x => x.ReservaGrupal?.Id == reservaGrupal.Id)
                    .ToList();

                foreach (var u in unidadesDelGrupo)
                    unidadesAgrupadas.Add(u.Aloj.Id);

                // Construir la lista de entradas respetando el orden de UnidadesGrupales
                // y agrupando las habs físicas bajo el Apart cuando corresponde.
                var unidades = new List<LimpiezaUnidadItem>();
                foreach (var unidadOrig in reservaGrupal.UnidadesGrupales)
                {
                    var alojOrig = unidadOrig.Alojamiento;
                    var det = apartDetalles.FirstOrDefault(d => d.AlojamientoApartId == alojOrig.Id);

                    if (det != null) // la unidad original es un Apart
                    {
                        bool hasApart = unidadesDelGrupo.Any(x => x.Aloj.Id == alojOrig.Id);
                        var apartTareas = hasApart
                            ? unidadesDelGrupo.First(x => x.Aloj.Id == alojOrig.Id).Tareas
                            : new List<string>();
                        var habEntries = new[] { det.AlojamientoHab1Id, det.AlojamientoHab2Id }
                            .Where(hId => unidadesDelGrupo.Any(x => x.Aloj.Id == hId))
                            .Select(hId => unidadesDelGrupo.First(x => x.Aloj.Id == hId))
                            .ToList();

                        if (hasApart || habEntries.Any())
                        {
                            unidades.Add(new LimpiezaUnidadItem
                            {
                                NombreAlojamiento = alojOrig.Nombre,
                                Tareas            = apartTareas,
                                EsSubtituloApart  = true,
                            });
                            foreach (var habEntry in habEntries)
                                unidades.Add(new LimpiezaUnidadItem
                                {
                                    NombreAlojamiento = habEntry.Aloj.Nombre,
                                    Tareas            = habEntry.Tareas,
                                    EsHabDeApart      = true,
                                });
                        }
                    }
                    else
                    {
                        if (unidadesDelGrupo.Any(x => x.Aloj.Id == alojOrig.Id))
                        {
                            var entry = unidadesDelGrupo.First(x => x.Aloj.Id == alojOrig.Id);
                            unidades.Add(new LimpiezaUnidadItem
                            {
                                NombreAlojamiento = entry.Aloj.Nombre,
                                Tareas            = entry.Tareas,
                            });
                        }
                    }
                }

                limpiezaItems.Add(new LimpiezaItem
                {
                    EsGrupo                = true,
                    CantUnidadesOriginales = reservaGrupal.UnidadesGrupales.Count,
                    Unidades               = unidades,
                });
            }
            else
            {
                limpiezaItems.Add(new LimpiezaItem { NombreAlojamiento = aloj.Nombre, Tareas = tareas });
                unidadesAgrupadas.Add(aloj.Id);
            }
        }

        // ── Pendientes a cobrar en 24 hs ─────────────────────────────────────
        var pendientes = reservas
            .Where(r => FS(r) == hoy || FS(r) == manana)
            .Select(r => new { r, saldo = r.MontoTotal - r.Pagos.Sum(p => p.Monto) })
            .Where(x => x.saldo > 0.01m)
            .OrderBy(x => FS(x.r))
            .ThenBy(x => x.r.Alojamiento.Nombre)
            .Select(x => new PendienteCobroItem
            {
                ReservaId         = x.r.Id,
                NombreHuesped     = x.r.NombreHuesped,
                NombreAlojamiento = NombreAloj(x.r.Alojamiento),
                FechaSalida       = FS(x.r),
                SaldoPendiente    = x.saldo,
                EsHoy             = FS(x.r) == hoy
            }).ToList();

        var vm = new InicioViewModel
        {
            Hoy                      = hoy,
            Manana                   = manana,
            CheckInsHoy              = checkInsHoy,
            CheckOutsHoy             = checkOutsHoy,
            PlazasOcupadasHoy        = plazas,
            ComensalesDesayunoHoy    = comensalesHoy,
            ComensalesDesayunoManana = comensales,
            UnidadesLibresHoy        = unidadesLibres,
            UnidadesOcupadasHoy      = unidadesOcupadas,
            LimpiezaDelDia           = limpiezaItems,
            PendientesCobro          = pendientes,
            CheckInsManana           = checkInsManana,
            CheckOutsManana          = checkOutsManana,
        };

        return View(vm);
    }

    private static int NumeroEnNombre(string nombre)
    {
        var m = Regex.Match(nombre, @"\d+");
        return m.Success ? int.Parse(m.Value) : 0;
    }
}
