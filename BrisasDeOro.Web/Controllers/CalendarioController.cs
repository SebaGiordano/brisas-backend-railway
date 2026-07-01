using System.Text.Json;
using BrisasDeOro.Web.Data;
using BrisasDeOro.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrisasDeOro.Web.Controllers;

[Authorize(Roles = "Administrador,Viewer")]
public class CalendarioController : Controller
{
    private readonly ApplicationDbContext _context;

    public CalendarioController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var desde = DateTime.Today.AddMonths(-7);
        var hasta = DateTime.Today.AddMonths(7);

        var reservas = await _context.Reservas
            .Where(r => r.Estado != EstadoReserva.Cancelada
                     && r.FechaIngreso < hasta
                     && r.FechaSalida  > desde)
            .Include(r => r.Pagos)
            .Include(r => r.Alojamiento)
            .Include(r => r.UnidadesGrupales)
                .ThenInclude(u => u.Alojamiento)
            .ToListAsync();

        var alojamientos = await _context.Alojamientos
            .Select(a => new { a.Id, a.Nombre, a.Capacidad })
            .ToListAsync();

        var reservasData = reservas.Select(r =>
        {
            var totalCobrado   = r.Pagos.Sum(p => p.Monto);
            var saldoPendiente = r.MontoTotal - totalCobrado;
            return new
            {
                id                = r.Id,
                alojamientoId     = r.AlojamientoId,
                nombreAlojamiento = r.Alojamiento.Nombre,
                nombreHuesped     = r.NombreHuesped,
                cantidadHuespedes = r.CantidadHuespedes,
                incluyeDesayuno   = r.IncluyeDesayuno,
                fechaIngreso      = r.FechaIngreso.ToString("yyyy-MM-dd"),
                fechaSalida       = r.FechaSalida.ToString("yyyy-MM-dd"),
                montoTotal        = r.MontoTotal,
                precioPorDia      = (r.FechaSalida - r.FechaIngreso).Days > 0 && !r.EsInvitacion
                                        ? Math.Round(r.MontoTotal / (r.FechaSalida - r.FechaIngreso).Days, 0)
                                        : (decimal?)null,
                esInvitacion      = r.EsInvitacion,
                esGrupal          = r.EsGrupal,
                unidadesGrupales  = r.EsGrupal
                    ? r.UnidadesGrupales.Select(u => new
                      {
                          alojamientoId = u.AlojamientoId,
                          nombre        = u.Alojamiento.Nombre,
                          personas      = u.CantidadHuespedes,
                          tipo          = u.Alojamiento.Tipo.ToString()
                      }).ToList()
                    : null,
                totalCobrado,
                saldoPendiente
            };
        }).ToList();

        var apartDetalles = await _context.ApartDetalles.ToListAsync();
        var apartMapData  = apartDetalles.Select(d => new
        {
            apartId = d.AlojamientoApartId,
            hab1Id  = d.AlojamientoHab1Id,
            hab2Id  = d.AlojamientoHab2Id
        });

        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        ViewBag.ReservasJson     = JsonSerializer.Serialize(reservasData, opts);
        ViewBag.AlojamientosJson = JsonSerializer.Serialize(alojamientos, opts);
        ViewBag.ApartMapJson     = JsonSerializer.Serialize(apartMapData,  opts);
        ViewBag.EsAdmin          = User.IsInRole("Administrador");

        return View();
    }
}
