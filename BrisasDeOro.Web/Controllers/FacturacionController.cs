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
        string? metodoPago, string? concepto,
        string orden = "desc",
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
            TabActiva           = tab
        };

        // ── Tab 1: Movimientos ────────────────────────────────────────────────

        var queryPagos = _context.Pagos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(titular))
            queryPagos = queryPagos.Where(p => p.Reserva.NombreHuesped.Contains(titular));

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
                Observaciones     = p.Observaciones
            })
            .ToListAsync();

        vm.Movimientos  = movimientos;
        vm.TotalPeriodo = movimientos.Sum(p => p.Monto);

        // ── Tab 2: Saldos pendientes ──────────────────────────────────────────

        var querySaldos = _context.Reservas
            .Where(r => r.Estado != EstadoReserva.Cancelada
                     && !r.EsInvitacion
                     && r.Pagos.Sum(p => p.Monto) < r.MontoTotal);

        if (!string.IsNullOrWhiteSpace(titularSaldos))
            querySaldos = querySaldos.Where(r => r.NombreHuesped.Contains(titularSaldos));

        vm.SaldosPendientes = await querySaldos
            .OrderBy(r => r.FechaSalida)
            .Select(r => new SaldoPendienteViewModel
            {
                ReservaId         = r.Id,
                NombreHuesped     = r.NombreHuesped,
                NombreAlojamiento = r.Alojamiento.Nombre,
                FechaIngreso      = r.FechaIngreso,
                FechaSalida       = r.FechaSalida,
                MontoTotal        = r.MontoTotal,
                TotalCobrado      = r.Pagos.Sum(p => p.Monto)
            })
            .ToListAsync();

        return View(vm);
    }

    // ── BuscarTitulares (AJAX autocomplete) ──────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> BuscarTitulares(string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 1)
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
}
