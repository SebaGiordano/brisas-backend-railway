using BrisasDeOro.Web.Data;
using BrisasDeOro.Web.Models;
using BrisasDeOro.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrisasDeOro.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class PagosController : Controller
{
    private readonly ApplicationDbContext _context;

    public PagosController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── AgregarPago GET ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> AgregarPago(int reservaId)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Alojamiento)
            .Include(r => r.Pagos)
            .FirstOrDefaultAsync(r => r.Id == reservaId);

        if (reserva == null) return NotFound();

        var totalCobrado   = reserva.Pagos.Sum(p => p.Monto);
        var saldoPendiente = reserva.MontoTotal - totalCobrado;

        var tienePagosPrevios = reserva.Pagos.Any();
        ViewBag.TienePagosPrevios = tienePagosPrevios;

        var vm = new AgregarPagoViewModel
        {
            ReservaId         = reserva.Id,
            NombreHuesped     = reserva.NombreHuesped,
            NombreAlojamiento = reserva.Alojamiento.Nombre,
            FechaIngreso      = reserva.FechaIngreso,
            FechaSalida       = reserva.FechaSalida,
            MontoTotal        = reserva.MontoTotal,
            TotalCobrado      = totalCobrado,
            SaldoPendiente    = saldoPendiente,
            Monto             = saldoPendiente,
            Fecha             = DateTime.Today,
            TipoPago          = saldoPendiente < 0 ? TipoPago.Ajuste
                              : tienePagosPrevios  ? TipoPago.Cancelacion
                              :                      TipoPago.Sena,
        };

        return View(vm);
    }

    // ── AgregarPago POST ──────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarPago(AgregarPagoViewModel model)
    {
        if (model.Monto == 0)
            ModelState.AddModelError(nameof(model.Monto), "El monto no puede ser cero.");
        else if (model.TipoPago != TipoPago.Ajuste && model.Monto < 0)
            ModelState.AddModelError(nameof(model.Monto), "El monto debe ser mayor a cero.");

        if (!ModelState.IsValid)
        {
            await RecargarDatosReserva(model);
            return View(model);
        }

        var pago = new Pago
        {
            ReservaId    = model.ReservaId,
            TipoPago     = model.TipoPago,
            Monto        = model.Monto,
            MetodoPago   = model.MetodoPago,
            Fecha        = DateTime.Now,
            Observaciones = model.Observaciones
        };

        _context.Pagos.Add(pago);
        await _context.SaveChangesAsync();

        TempData["Mensaje"]     = $"Pago de ${model.Monto:N0} registrado correctamente.";
        TempData["MensajeTipo"] = "success";

        return RedirectToAction("Details", "Reservas", new { id = model.ReservaId });
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task RecargarDatosReserva(AgregarPagoViewModel vm)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Alojamiento)
            .Include(r => r.Pagos)
            .FirstOrDefaultAsync(r => r.Id == vm.ReservaId);

        if (reserva == null) return;

        var totalCobrado = reserva.Pagos.Sum(p => p.Monto);

        ViewBag.TienePagosPrevios = reserva.Pagos.Any();

        vm.NombreHuesped     = reserva.NombreHuesped;
        vm.NombreAlojamiento = reserva.Alojamiento.Nombre;
        vm.FechaIngreso      = reserva.FechaIngreso;
        vm.FechaSalida       = reserva.FechaSalida;
        vm.MontoTotal        = reserva.MontoTotal;
        vm.TotalCobrado      = totalCobrado;
        vm.SaldoPendiente    = reserva.MontoTotal - totalCobrado;
    }
}
