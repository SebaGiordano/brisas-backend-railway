using BrisasDeOro.Web.Data;
using BrisasDeOro.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrisasDeOro.Web.Controllers;

[Route("api/disponibilidad")]
[ApiController]
[AllowAnonymous]
public class DisponibilidadController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DisponibilidadController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime fechaIngreso,
        [FromQuery] DateTime fechaEgreso,
        [FromQuery] int cantidadPersonas)
    {
        if (fechaIngreso == default || fechaEgreso == default)
            return BadRequest(new { error = "fechaIngreso y fechaEgreso son requeridos." });

        if (fechaIngreso >= fechaEgreso)
            return BadRequest(new { error = "fechaIngreso debe ser anterior a fechaEgreso." });

        if (cantidadPersonas < 1)
            return BadRequest(new { error = "cantidadPersonas debe ser al menos 1." });

        var alojamientos = await _db.Alojamientos
            .Where(a => a.Activo)
            .ToListAsync();

        var apartDetalles = await _db.ApartDetalles.ToListAsync();

        // Alojamientos con reserva activa que solapa el rango pedido.
        // El día de egreso de una reserva existente SÍ puede ser ingreso de una nueva
        // → se usa FechaSalida > fechaIngreso (no >=).
        var ocupadosDirectos = (await _db.Reservas
            .Where(r => r.Estado != EstadoReserva.Cancelada
                     && r.FechaIngreso < fechaEgreso
                     && r.FechaSalida > fechaIngreso)
            .Select(r => r.AlojamientoId)
            .ToListAsync())
            .ToHashSet();

        // Expandir con lógica de Apart:
        // - Apart reservado  → sus habitaciones quedan bloqueadas para el grupo Habitaciones
        // - Habitación reservada → su Apart queda bloqueado para el grupo Aparts
        var ocupados = new HashSet<int>(ocupadosDirectos);
        foreach (var ad in apartDetalles)
        {
            if (ocupadosDirectos.Contains(ad.AlojamientoApartId))
            {
                ocupados.Add(ad.AlojamientoHab1Id);
                ocupados.Add(ad.AlojamientoHab2Id);
            }
            if (ocupadosDirectos.Contains(ad.AlojamientoHab1Id) ||
                ocupadosDirectos.Contains(ad.AlojamientoHab2Id))
            {
                ocupados.Add(ad.AlojamientoApartId);
            }
        }

        Alojamiento? ByNombre(string nombre) =>
            alojamientos.FirstOrDefault(a => a.Nombre == nombre);

        bool EsDisponible(Alojamiento? a) =>
            a is not null && !ocupados.Contains(a.Id) && a.Capacidad >= cantidadPersonas;

        // ── Habitaciones (Hab. 4-11) ──────────────────────────────────────────
        var habNombres = new[] {
            "Habitación 4", "Habitación 5", "Habitación 6", "Habitación 7",
            "Habitación 8", "Habitación 9", "Habitación 10", "Habitación 11"
        };
        int habDisponibles = habNombres
            .Select(n => ByNombre(n))
            .Count(EsDisponible);

        // ── Aparts (Apart 1 y 2) — solo válidos para 3 a 5 personas ──────────
        int apartDisponibles = 0;
        if (cantidadPersonas >= 3 && cantidadPersonas <= 5)
        {
            apartDisponibles = new[] { "Apart 1", "Apart 2" }
                .Select(n => ByNombre(n))
                .Count(EsDisponible);
        }

        // ── Cabaña chica (Cabaña 7) ───────────────────────────────────────────
        int cabChicaDisponibles = EsDisponible(ByNombre("Cabaña 7")) ? 1 : 0;

        // ── Cabaña mediana (Cabañas 1-6) ──────────────────────────────────────
        var cabMedianaNombres = new[] {
            "Cabaña 1", "Cabaña 2", "Cabaña 3",
            "Cabaña 4", "Cabaña 5", "Cabaña 6"
        };
        int cabMedianaDisponibles = cabMedianaNombres
            .Select(n => ByNombre(n))
            .Count(EsDisponible);

        // ── Cabaña grande (Cabaña 8) ──────────────────────────────────────────
        int cabGrandeDisponibles = EsDisponible(ByNombre("Cabaña 8")) ? 1 : 0;

        return Ok(new
        {
            habitaciones = new
            {
                disponible        = habDisponibles > 0,
                unidadesDisponibles = habDisponibles
            },
            aparts = new
            {
                disponible        = apartDisponibles > 0,
                unidadesDisponibles = apartDisponibles
            },
            cabanaChica = new
            {
                disponible        = cabChicaDisponibles > 0,
                unidadesDisponibles = cabChicaDisponibles
            },
            cabanaMediana = new
            {
                disponible        = cabMedianaDisponibles > 0,
                unidadesDisponibles = cabMedianaDisponibles
            },
            cabanaGrande = new
            {
                disponible        = cabGrandeDisponibles > 0,
                unidadesDisponibles = cabGrandeDisponibles
            }
        });
    }
}
