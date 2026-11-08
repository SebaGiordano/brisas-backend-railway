using BrisasDeOro.Web.Data;
using BrisasDeOro.Web.Helpers;
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
    public async Task<IActionResult> AgregarPago(int reservaId, string? from, string? returnUrl)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Alojamiento)
            .Include(r => r.Pagos)
            .Include(r => r.UnidadesGrupales)
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
            NombreAlojamiento = reserva.EsGrupal && reserva.UnidadesGrupales.Count > 0
                                    ? $"Grupo ({reserva.UnidadesGrupales.Count} unidades)"
                                    : reserva.Alojamiento.Nombre,
            FechaIngreso      = reserva.FechaIngreso,
            FechaSalida       = reserva.FechaSalida,
            MontoTotal        = reserva.MontoTotal,
            TotalCobrado      = totalCobrado,
            SaldoPendiente    = saldoPendiente,
            Monto             = saldoPendiente,
            // Reserva ya terminada (ej.: carga de temporadas anteriores): la fecha arranca vacía y hay que elegirla
            Fecha = reserva.FechaSalida.Date < DateTime.Today ? null : DateTime.Today,
            TipoPago          = saldoPendiente < 0 ? TipoPago.Ajuste
                              : tienePagosPrevios  ? TipoPago.Cancelacion
                              :                      TipoPago.Sena,
            From              = from,
            ReturnUrl         = returnUrl,
        };

        // La seña casi siempre se paga por transferencia (el cliente no está en el complejo)
        vm.MetodoPago = vm.TipoPago == TipoPago.Sena ? MetodoPago.Transferencia : MetodoPago.Efectivo;

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
            // Día elegido por el usuario + hora del momento del registro
            Fecha = DateTime.SpecifyKind(model.Fecha!.Value.Date + DateTime.Now.TimeOfDay, DateTimeKind.Local),
            Observaciones = model.Observaciones
        };

        _context.Pagos.Add(pago);
        await _context.SaveChangesAsync();

        TempData["Mensaje"]     = $"Pago de {Formato.Moneda(model.Monto)} registrado correctamente.";
        TempData["MensajeTipo"] = "success";

        var routeValues = new Dictionary<string, string?> { ["id"] = model.ReservaId.ToString() };
        if (!string.IsNullOrEmpty(model.From))
        {
            routeValues["from"] = model.From;
            if (model.From == "facturacion" && Url.IsLocalUrl(model.ReturnUrl))
                routeValues["returnUrl"] = model.ReturnUrl;
        }

        return RedirectToAction("Details", "Reservas", routeValues);
    }

    // ── EditarPago POST ───────────────────────────────────────────────────────
    // Edita fecha, método y observaciones. Monto y concepto no se modifican.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarPago(int id, DateTime? fecha, MetodoPago metodoPago,
                                                string? observaciones, string? from, string? returnUrl)
    {
        var pago = await _context.Pagos.FindAsync(id);
        if (pago == null) return NotFound();

        var routeValues = new Dictionary<string, string?> { ["id"] = pago.ReservaId.ToString() };
        if (!string.IsNullOrEmpty(from))
        {
            routeValues["from"] = from;
            if (from == "facturacion" && Url.IsLocalUrl(returnUrl))
                routeValues["returnUrl"] = returnUrl;
        }

        if (fecha == null)
        {
            TempData["Mensaje"]     = "Seleccioná la fecha en que se realizó el pago.";
            TempData["MensajeTipo"] = "danger";
            return RedirectToAction("Details", "Reservas", routeValues);
        }

        // Se conserva la hora original del registro; solo cambia el día
        pago.Fecha         = DateTime.SpecifyKind(fecha.Value.Date + pago.Fecha.TimeOfDay, DateTimeKind.Local);
        pago.MetodoPago    = metodoPago;
        pago.Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        await _context.SaveChangesAsync();

        TempData["Mensaje"]     = "Pago actualizado correctamente.";
        TempData["MensajeTipo"] = "success";
        return RedirectToAction("Details", "Reservas", routeValues);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task RecargarDatosReserva(AgregarPagoViewModel vm)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Alojamiento)
            .Include(r => r.Pagos)
            .Include(r => r.UnidadesGrupales)
            .FirstOrDefaultAsync(r => r.Id == vm.ReservaId);

        if (reserva == null) return;

        var totalCobrado = reserva.Pagos.Sum(p => p.Monto);

        ViewBag.TienePagosPrevios = reserva.Pagos.Any();

        vm.NombreHuesped     = reserva.NombreHuesped;
        vm.NombreAlojamiento = reserva.EsGrupal && reserva.UnidadesGrupales.Count > 0
                                   ? $"Grupo ({reserva.UnidadesGrupales.Count} unidades)"
                                   : reserva.Alojamiento.Nombre;
        vm.FechaIngreso      = reserva.FechaIngreso;
        vm.FechaSalida       = reserva.FechaSalida;
        vm.MontoTotal        = reserva.MontoTotal;
        vm.TotalCobrado      = totalCobrado;
        vm.SaldoPendiente    = reserva.MontoTotal - totalCobrado;
    }
}
