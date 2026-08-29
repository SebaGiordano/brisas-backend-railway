using BrisasDeOro.Web.Data;
using BrisasDeOro.Web.Models;
using BrisasDeOro.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrisasDeOro.Web.Controllers;

[Authorize(Roles = "Administrador,Viewer")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index(
        int? mes, int? anno, DateTime? desde, DateTime? hasta,
        int? mesComp, int? annoComp, DateTime? desdeComp, DateTime? hastaComp,
        bool comparar = false)
    {
        var hoy = DateTime.Today;

        var (pDesde, pHasta) = ResolvePeriod(mes, anno, desde, hasta, hoy.Month, hoy.Year);

        // Años disponibles para el desplegable
        var annosList = await _context.Reservas
            .Select(r => r.FechaIngreso.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();
        if (!annosList.Contains(hoy.Year)) annosList.Insert(0, hoy.Year);

        // Datos reutilizables entre períodos
        var alojamientos  = await _context.Alojamientos.Where(a => a.Activo).ToListAsync();
        var apartDetalles = await _context.ApartDetalles.ToListAsync();

        bool usaRango     = desde.HasValue     && hasta.HasValue;
        bool usaRangoComp = desdeComp.HasValue && hastaComp.HasValue;

        var vm = new DashboardViewModel
        {
            Mes              = usaRango     ? (int?)null : (mes  ?? hoy.Month),
            Anno             = usaRango     ? (int?)null : (anno ?? hoy.Year),
            Desde            = desde,
            Hasta            = hasta,
            Comparar         = comparar,
            MesComp          = usaRangoComp ? (int?)null : mesComp,
            AnnoComp         = usaRangoComp ? (int?)null : annoComp,
            DesdeComp        = desdeComp,
            HastaComp        = hastaComp,
            AnnosDisponibles = annosList,
            Periodo          = await CalcularPeriodoAsync(pDesde, pHasta, alojamientos, apartDetalles),
        };

        if (comparar)
        {
            int defMesComp  = mesComp  ?? (pDesde.Month == 1 ? 12 : pDesde.Month - 1);
            int defAnnoComp = annoComp ?? (pDesde.Month == 1 ? pDesde.Year - 1 : pDesde.Year);
            var (cDesde, cHasta) = ResolvePeriod(mesComp, annoComp, desdeComp, hastaComp, defMesComp, defAnnoComp);
            vm.PeriodoComparacion = await CalcularPeriodoAsync(cDesde, cHasta, alojamientos, apartDetalles);
        }

        return View(vm);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (DateTime Desde, DateTime Hasta) ResolvePeriod(
        int? mes, int? anno, DateTime? desde, DateTime? hasta,
        int defaultMes, int defaultAnno)
    {
        if (desde.HasValue && hasta.HasValue)
            return (desde.Value.Date, hasta.Value.Date);

        var m = mes  ?? defaultMes;
        var y = anno ?? defaultAnno;
        var d = new DateTime(y, m, 1);
        return (d, d.AddMonths(1).AddDays(-1));
    }

    private async Task<DashboardPeriodoViewModel> CalcularPeriodoAsync(
        DateTime desde, DateTime hasta,
        List<Alojamiento> alojamientos,
        List<ApartDetalle> apartDetalles)
    {
        var hastaExcl  = hasta.Date.AddDays(1);
        // Días reales del período (inclusive): ej. 01/05–31/05 = 31 días, feb bisiesto = 29, etc.
        var periodDays = (hasta.Date - desde.Date).Days + 1;

        var reservas = await _context.Reservas
            .Include(r => r.Pagos)
            .Include(r => r.UnidadesGrupales)
            .Where(r => r.Estado       != EstadoReserva.Cancelada
                     && !r.EsInvitacion
                     && r.FechaIngreso  < hastaExcl
                     && r.FechaSalida   > desde.Date)
            .ToListAsync();

        // Reservas cuyo check-in cae dentro del período
        var enPeriodo = reservas
            .Where(r => r.FechaIngreso.Date >= desde.Date && r.FechaIngreso.Date <= hasta.Date)
            .ToList();

        // ── Tarjetas de resumen ───────────────────────────────────────────────
        decimal totalIngresos  = enPeriodo.Sum(r => r.Pagos.Sum(p => p.Monto));
        int     cantReservas   = enPeriodo.Count;
        decimal promedioNoches = cantReservas > 0
            ? Math.Round((decimal)enPeriodo.Average(r => (r.FechaSalida - r.FechaIngreso).TotalDays), 1)
            : 0;

        // ── Ingresos por método — por fecha de ingreso ────────────────────────
        var ingresosFI = new IngresoMetodoPagoViewModel();
        foreach (var r in enPeriodo)
            foreach (var p in r.Pagos)
                AcumularPago(ingresosFI, p.MetodoPago, p.Monto);

        // ── Ingresos por método — prorrateo de pagos según noches en período ──
        var ingresosPro = new IngresoMetodoPagoViewModel();
        foreach (var r in reservas)
        {
            var totalNights  = (r.FechaSalida - r.FechaIngreso).TotalDays;
            if (totalNights <= 0) continue;

            var overlapStart = r.FechaIngreso.Date > desde.Date ? r.FechaIngreso.Date : desde.Date;
            var overlapEnd   = r.FechaSalida.Date  < hastaExcl  ? r.FechaSalida.Date  : hastaExcl;
            var overlapDays  = (overlapEnd - overlapStart).TotalDays;
            if (overlapDays <= 0) continue;

            var frac = (decimal)(overlapDays / totalNights);
            foreach (var p in r.Pagos)
                AcumularPago(ingresosPro, p.MetodoPago, Math.Round(p.Monto * frac, 2));
        }

        // ── Ocupación ─────────────────────────────────────────────────────────
        var habitaciones = alojamientos.Where(a => a.Tipo == TipoAlojamiento.Habitacion).ToList();
        var cabanas      = alojamientos.Where(a => a.Tipo == TipoAlojamiento.Cabaña).ToList();

        var habToApart = new Dictionary<int, int>();
        foreach (var det in apartDetalles)
        {
            habToApart[det.AlojamientoHab1Id] = det.AlojamientoApartId;
            habToApart[det.AlojamientoHab2Id] = det.AlojamientoApartId;
        }

        int DiasOcupados(int alojId, int? altApartId)
        {
            return reservas
                .Where(r => r.AlojamientoId == alojId
                         || (altApartId.HasValue && r.AlojamientoId == altApartId.Value)
                         || (r.EsGrupal && r.UnidadesGrupales.Any(u => u.AlojamientoId == alojId))
                         || (altApartId.HasValue && r.EsGrupal && r.UnidadesGrupales.Any(u => u.AlojamientoId == altApartId.Value)))
                .Sum(r =>
                {
                    var s = r.FechaIngreso.Date > desde.Date ? r.FechaIngreso.Date : desde.Date;
                    var e = r.FechaSalida.Date  < hastaExcl  ? r.FechaSalida.Date  : hastaExcl;
                    return Math.Max(0, (int)(e - s).TotalDays);
                });
        }

        int diasHabOcup = habitaciones.Sum(h =>
            DiasOcupados(h.Id, habToApart.TryGetValue(h.Id, out var aId) ? aId : (int?)null));
        int diasCabOcup = cabanas.Sum(c => DiasOcupados(c.Id, null));

        int totalDiasHab = habitaciones.Count * periodDays;
        int totalDiasCab = cabanas.Count      * periodDays;

        var ocupacion = new OcupacionDetalleViewModel
        {
            DiasHabOcupados = diasHabOcup,
            DiasCabOcupados = diasCabOcup,
            DiasHabTotal    = totalDiasHab,
            DiasCabTotal    = totalDiasCab,
            PctHabitaciones = totalDiasHab > 0
                ? Math.Round((decimal)diasHabOcup / totalDiasHab * 100, 1) : 0,
            PctCabanas      = totalDiasCab > 0
                ? Math.Round((decimal)diasCabOcup / totalDiasCab * 100, 1) : 0,
            PctTotal        = (totalDiasHab + totalDiasCab) > 0
                ? Math.Round((decimal)(diasHabOcup + diasCabOcup) / (totalDiasHab + totalDiasCab) * 100, 1) : 0,
        };

        // ── Ocupación de plazas ───────────────────────────────────────────────
        // Denominador: capacidad de todas las habs (incluye componentes de Aparts) × días.
        // Numerador:   CantidadHuespedes × días solapados.
        // Apart se trata como Habitacion (sus habs componentes ya están en el denominador).
        var alojMap = alojamientos.ToDictionary(a => a.Id);

        int plazasHabOcup = 0;
        int plazasCabOcup = 0;

        foreach (var r in reservas)
        {
            var os = r.FechaIngreso.Date > desde.Date ? r.FechaIngreso.Date : desde.Date;
            var oe = r.FechaSalida.Date  < hastaExcl  ? r.FechaSalida.Date  : hastaExcl;
            var od = (oe - os).Days;
            if (od <= 0) continue;

            if (r.EsGrupal && r.UnidadesGrupales.Count > 0)
            {
                foreach (var u in r.UnidadesGrupales)
                {
                    if (!alojMap.TryGetValue(u.AlojamientoId, out var uAloj)) continue;
                    if (uAloj.Tipo == TipoAlojamiento.Habitacion ||
                        uAloj.Tipo == TipoAlojamiento.Apart)       plazasHabOcup += u.CantidadHuespedes * od;
                    else if (uAloj.Tipo == TipoAlojamiento.Cabaña) plazasCabOcup += u.CantidadHuespedes * od;
                }
            }
            else
            {
                if (!alojMap.TryGetValue(r.AlojamientoId, out var aloj)) continue;

                if (aloj.Tipo == TipoAlojamiento.Habitacion ||
                    aloj.Tipo == TipoAlojamiento.Apart)       plazasHabOcup += r.CantidadHuespedes * od;
                else if (aloj.Tipo == TipoAlojamiento.Cabaña) plazasCabOcup += r.CantidadHuespedes * od;
            }
        }

        int plazasHabDisp = habitaciones.Sum(h => h.Capacidad) * periodDays;
        int plazasCabDisp = cabanas.Sum(c => c.Capacidad)      * periodDays;

        ocupacion.PlazasHabOcupadas     = plazasHabOcup;
        ocupacion.PlazasHabDisponibles  = plazasHabDisp;
        ocupacion.PlazasCabOcupadas     = plazasCabOcup;
        ocupacion.PlazasCabDisponibles  = plazasCabDisp;
        ocupacion.PctPlazasHabitaciones = plazasHabDisp > 0
            ? Math.Round((decimal)plazasHabOcup / plazasHabDisp * 100, 1) : 0;
        ocupacion.PctPlazasCabanas      = plazasCabDisp > 0
            ? Math.Round((decimal)plazasCabOcup / plazasCabDisp * 100, 1) : 0;
        var tplOcup = plazasHabOcup + plazasCabOcup;
        var tplDisp = plazasHabDisp + plazasCabDisp;
        ocupacion.PctPlazasTotal        = tplDisp > 0
            ? Math.Round((decimal)tplOcup / tplDisp * 100, 1) : 0;

        // ── Ocupación de la Casa (estadística aparte, no participa de reservas
        // grupales, así que no hace falta contemplar ese caso) ────────────────
        var casas = alojamientos.Where(a => a.Tipo == TipoAlojamiento.Casa).ToList();

        int diasCasaOcup   = casas.Sum(c => DiasOcupados(c.Id, null));
        int totalDiasCasa  = casas.Count * periodDays;

        int plazasCasaOcup = 0;
        foreach (var r in reservas)
        {
            var os = r.FechaIngreso.Date > desde.Date ? r.FechaIngreso.Date : desde.Date;
            var oe = r.FechaSalida.Date  < hastaExcl  ? r.FechaSalida.Date  : hastaExcl;
            var od = (oe - os).Days;
            if (od <= 0) continue;

            if (!r.EsGrupal && alojMap.TryGetValue(r.AlojamientoId, out var aloj)
                && aloj.Tipo == TipoAlojamiento.Casa)
                plazasCasaOcup += r.CantidadHuespedes * od;
        }
        int plazasCasaDisp = casas.Sum(c => c.Capacidad) * periodDays;

        var ocupacionCasa = new OcupacionCasaViewModel
        {
            DiasOcupados      = diasCasaOcup,
            DiasTotal         = totalDiasCasa,
            PctDias           = totalDiasCasa > 0 ? Math.Round((decimal)diasCasaOcup / totalDiasCasa * 100, 1) : 0,
            PlazasOcupadas    = plazasCasaOcup,
            PlazasDisponibles = plazasCasaDisp,
            PctPlazas         = plazasCasaDisp > 0 ? Math.Round((decimal)plazasCasaOcup / plazasCasaDisp * 100, 1) : 0
        };

        // ── Canales de origen ─────────────────────────────────────────────────
        var canales = enPeriodo
            .GroupBy(r => string.IsNullOrEmpty(r.CanalOrigen) ? "Sin especificar" : r.CanalOrigen)
            .Select(g => new CanalReservaViewModel { Canal = g.Key, Cantidad = g.Count() })
            .OrderByDescending(c => c.Cantidad)
            .ToList();

        var totalCanales = canales.Sum(c => c.Cantidad);
        foreach (var c in canales)
            c.Porcentaje = totalCanales > 0
                ? Math.Round((decimal)c.Cantidad / totalCanales * 100, 1) : 0;

        // ── Label ─────────────────────────────────────────────────────────────
        bool esMesCompleto = desde.Day == 1
            && hasta.Day == DateTime.DaysInMonth(hasta.Year, hasta.Month)
            && desde.Year == hasta.Year && desde.Month == hasta.Month;

        var meses = new[] { "Enero","Febrero","Marzo","Abril","Mayo","Junio",
                            "Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre" };
        string label = esMesCompleto
            ? $"{meses[desde.Month - 1]} {desde.Year}"
            : $"{desde:dd/MM/yyyy} – {hasta:dd/MM/yyyy}";

        return new DashboardPeriodoViewModel
        {
            Desde                = desde,
            Hasta                = hasta,
            Label                = label,
            TotalIngresos        = Math.Round(totalIngresos, 0),
            PctOcupacion         = ocupacion.PctTotal,
            CantidadReservas     = cantReservas,
            PromedioNoches       = promedioNoches,
            IngresosFechaIngreso = ingresosFI,
            IngresosProrrateo    = ingresosPro,
            Ocupacion            = ocupacion,
            OcupacionCasa        = ocupacionCasa,
            Canales              = canales,
        };
    }

    private static void AcumularPago(IngresoMetodoPagoViewModel vm, MetodoPago metodo, decimal monto)
    {
        switch (metodo)
        {
            case MetodoPago.Efectivo:       vm.Efectivo       += monto; break;
            case MetodoPago.Transferencia:  vm.Transferencia  += monto; break;
            case MetodoPago.TarjetaCredito: vm.TarjetaCredito += monto; break;
            case MetodoPago.TarjetaDebito:  vm.TarjetaDebito  += monto; break;
        }
    }
}
