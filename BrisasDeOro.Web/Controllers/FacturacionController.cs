using BrisasDeOro.Web.Data;
using BrisasDeOro.Web.Models;
using BrisasDeOro.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrisasDeOro.Web.Controllers;

[Authorize(Roles = "Administrador,Viewer")]
public class FacturacionController : Controller
{
    private readonly ApplicationDbContext _context;

    public FacturacionController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index(
        string? titular, string? titularSaldos,
        string? desde, string? hasta,
        string? desdeSaldos, string? hastaSaldos,
        string? metodoPago, string? concepto,
        string orden = "desc",
        string ordenSaldos = "asc",
        string tab = "movimientos")
    {
        var vm = new FacturacionViewModel
        {
            FiltroTitular       = titular,
            FiltroDesde         = desde,
            FiltroHasta         = hasta,
            FiltroMetodo        = metodoPago,
            FiltroConcepto      = concepto,
            FiltroOrden         = orden,
            FiltroTitularSaldos = titularSaldos,
            FiltroDesdeSaldos   = desdeSaldos,
            FiltroHastaSaldos   = hastaSaldos,
            FiltroOrdenSaldos   = ordenSaldos,
            TabActiva           = tab
        };

        // ── Tab 1: Movimientos ────────────────────────────────────────────────

        var queryPagos = _context.Pagos.AsQueryable();

        Dictionary<int, string> vinculacionMovimientos = new();

        if (!string.IsNullOrWhiteSpace(titular))
        {
            var coincidenNombre = await _context.Reservas
                .Where(r => r.NombreHuesped.Contains(titular))
                .Select(r => new { r.Id, r.NombreHuesped, r.GrupoVinculadoId })
                .ToListAsync();

            var idsCoincidenNombre = coincidenNombre.Select(x => x.Id).ToHashSet();

            var nombresDirectosPorGrupo = coincidenNombre
                .Where(x => x.GrupoVinculadoId.HasValue)
                .GroupBy(x => x.GrupoVinculadoId!.Value)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.NombreHuesped).Distinct()));

            var gruposCoincidenEtiqueta = await _context.GruposVinculados
                .Where(g => g.Etiqueta != null && g.Etiqueta.Contains(titular))
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
                        vinculacionMovimientos[r.Id] =
                            nombresDirectosPorGrupo.TryGetValue(r.GrupoVinculadoId!.Value, out var nombresReales) ? nombresReales
                            : etiquetaPorGrupo.TryGetValue(r.GrupoVinculadoId!.Value, out var etiq) ? etiq
                            : titular;
                    }
                }
            }

            queryPagos = queryPagos.Where(p => idsFinal.Contains(p.ReservaId));
        }

        if (!string.IsNullOrEmpty(desde) && DateTime.TryParse(desde, out var d))
            queryPagos = queryPagos.Where(p => p.Fecha >= d.Date);

        if (!string.IsNullOrEmpty(hasta) && DateTime.TryParse(hasta, out var h))
            queryPagos = queryPagos.Where(p => p.Fecha < h.Date.AddDays(1));

        if (!string.IsNullOrEmpty(metodoPago) && Enum.TryParse<MetodoPago>(metodoPago, out var mp))
            queryPagos = queryPagos.Where(p => p.MetodoPago == mp);

        if (!string.IsNullOrEmpty(concepto) && Enum.TryParse<TipoPago>(concepto, out var tp))
            queryPagos = queryPagos.Where(p => p.TipoPago == tp);

        var movimientos = await (orden == "asc"
                ? queryPagos.OrderBy(p => p.Fecha)
                : queryPagos.OrderByDescending(p => p.Fecha))
            .Select(p => new MovimientoViewModel
            {
                ReservaId         = p.ReservaId,
                Fecha             = p.Fecha,
                NombreHuesped     = p.Reserva.NombreHuesped,
                NombreAlojamiento = p.Reserva.Alojamiento.Nombre,
                TipoPago          = p.TipoPago,
                MetodoPago        = p.MetodoPago,
                Monto             = p.Monto,
                Observaciones     = p.Observaciones,
                EsGrupal          = p.Reserva.EsGrupal,
                Unidades          = p.Reserva.UnidadesGrupales
                    .Select(u => new UnidadGrupalResumen
                    {
                        Nombre   = u.Alojamiento.Nombre,
                        Personas = u.CantidadHuespedes
                    }).ToList()
            })
            .ToListAsync();

        foreach (var m in movimientos)
            if (vinculacionMovimientos.TryGetValue(m.ReservaId, out var motivo))
                m.VinculadaConNombre = motivo;

        vm.Movimientos  = movimientos;
        vm.TotalPeriodo = movimientos.Sum(p => p.Monto);

        // ── Tab 2: Saldos pendientes ──────────────────────────────────────────

        var querySaldos = _context.Reservas
            .Where(r => r.Estado != EstadoReserva.Cancelada
                     && !r.EsInvitacion
                     && r.Pagos.Sum(p => p.Monto) < r.MontoTotal);

        Dictionary<int, string> vinculacionSaldos = new();

        if (!string.IsNullOrWhiteSpace(titularSaldos))
        {
            var coincidenNombreS = await _context.Reservas
                .Where(r => r.NombreHuesped.Contains(titularSaldos))
                .Select(r => new { r.Id, r.NombreHuesped, r.GrupoVinculadoId })
                .ToListAsync();

            var idsCoincidenNombreS = coincidenNombreS.Select(x => x.Id).ToHashSet();

            var nombresDirectosPorGrupoS = coincidenNombreS
                .Where(x => x.GrupoVinculadoId.HasValue)
                .GroupBy(x => x.GrupoVinculadoId!.Value)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.NombreHuesped).Distinct()));

            var gruposCoincidenEtiquetaS = await _context.GruposVinculados
                .Where(g => g.Etiqueta != null && g.Etiqueta.Contains(titularSaldos))
                .Select(g => new { g.Id, g.Etiqueta })
                .ToListAsync();
            var etiquetaPorGrupoS = gruposCoincidenEtiquetaS.ToDictionary(g => g.Id, g => g.Etiqueta!);

            var todosLosGruposS = etiquetaPorGrupoS.Keys.Union(nombresDirectosPorGrupoS.Keys).Distinct().ToList();

            var idsFinalS = idsCoincidenNombreS;

            if (todosLosGruposS.Any())
            {
                var reservasDeGruposS = await _context.Reservas
                    .Where(r => r.GrupoVinculadoId.HasValue && todosLosGruposS.Contains(r.GrupoVinculadoId.Value))
                    .Select(r => new { r.Id, r.NombreHuesped, r.GrupoVinculadoId })
                    .ToListAsync();

                foreach (var r in reservasDeGruposS)
                {
                    if (!idsFinalS.Contains(r.Id))
                    {
                        idsFinalS.Add(r.Id);
                        vinculacionSaldos[r.Id] =
                            nombresDirectosPorGrupoS.TryGetValue(r.GrupoVinculadoId!.Value, out var nombresRealesS) ? nombresRealesS
                            : etiquetaPorGrupoS.TryGetValue(r.GrupoVinculadoId!.Value, out var etiqS) ? etiqS
                            : titularSaldos;
                    }
                }
            }

            querySaldos = querySaldos.Where(r => idsFinalS.Contains(r.Id));
        }

        if (!string.IsNullOrEmpty(desdeSaldos) && DateTime.TryParse(desdeSaldos, out var dS))
            querySaldos = querySaldos.Where(r => r.FechaIngreso >= dS.Date);

        if (!string.IsNullOrEmpty(hastaSaldos) && DateTime.TryParse(hastaSaldos, out var hS))
            querySaldos = querySaldos.Where(r => r.FechaIngreso <= hS.Date);

        var querySaldosOrdenada = (ordenSaldos == "desc")
            ? querySaldos.OrderByDescending(r => r.FechaIngreso)
            : querySaldos.OrderBy(r => r.FechaIngreso);

        vm.SaldosPendientes = await querySaldosOrdenada
            .Select(r => new SaldoPendienteViewModel
            {
                ReservaId         = r.Id,
                NombreHuesped     = r.NombreHuesped,
                NombreAlojamiento = r.Alojamiento.Nombre,
                FechaIngreso      = r.FechaIngreso,
                FechaSalida       = r.FechaSalida,
                MontoTotal        = r.MontoTotal,
                TotalCobrado      = r.Pagos.Sum(p => p.Monto),
                EsGrupal          = r.EsGrupal,
                Unidades          = r.UnidadesGrupales
                    .Select(u => new UnidadGrupalResumen
                    {
                        Nombre   = u.Alojamiento.Nombre,
                        Personas = u.CantidadHuespedes
                    }).ToList()
            })
            .ToListAsync();

        foreach (var s in vm.SaldosPendientes)
            if (vinculacionSaldos.TryGetValue(s.ReservaId, out var motivo))
                s.VinculadaConNombre = motivo;

        return View(vm);
    }

    // ── BuscarTitulares (AJAX autocomplete) ──────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> BuscarTitulares(string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 1)
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
}
