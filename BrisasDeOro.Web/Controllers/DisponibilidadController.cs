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
        var reservasEnRango = await _db.Reservas
            .Where(r => r.Estado != EstadoReserva.Cancelada
                     && r.FechaIngreso < fechaEgreso
                     && r.FechaSalida > fechaIngreso)
            .Select(r => new { r.Id, r.AlojamientoId, r.EsGrupal })
            .ToListAsync();

        var reservaIdsEnRango = reservasEnRango.Select(r => r.Id).ToList();

        // Grupales: las unidades reales están en ReservaAlojamientos, no en AlojamientoId
        // (que en una reserva grupal es solo la primera unidad del grupo).
        var ocupadosGrupales = await _db.ReservaAlojamientos
            .Where(ra => reservaIdsEnRango.Contains(ra.ReservaId))
            .Select(ra => ra.AlojamientoId)
            .ToListAsync();

        var ocupadosDirectos = reservasEnRango
            .Where(r => !r.EsGrupal)
            .Select(r => r.AlojamientoId)
            .Concat(ocupadosGrupales)
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

        // ── Regla de oferta por tamaño de grupo: qué unidades se ofrecen como
        // opción individual según cantidadPersonas (no afecta a `libres`, que
        // alimenta las combinaciones y siempre incluye todas las unidades libres).
        bool Ofrecible(string categoria, int n) => categoria switch
        {
            "habitaciones"  => true,
            "aparts"        => n >= 3 && n <= 5,
            "cabanaChica"   => n >= 1 && n <= 3,
            "cabanaMediana" => n >= 1 && n <= 6,
            "cabanaGrande"  => n >= 4 && n <= 8,
            "casa"          => n >= 4 && n <= 10,
            _ => false
        };

        // ── Habitaciones (Hab. 4-11) ──────────────────────────────────────────
        bool ofrecibleHabitaciones = Ofrecible("habitaciones", cantidadPersonas);
        var habNombres = new[] {
            "Habitación 4", "Habitación 5", "Habitación 6", "Habitación 7",
            "Habitación 8", "Habitación 9", "Habitación 10", "Habitación 11"
        };
        int habDisponibles = !ofrecibleHabitaciones ? 0 : habNombres
            .Select(n => ByNombre(n))
            .Count(EsDisponible);

        // ── Aparts (Apart 1 y 2) — solo ofrecibles de 3 a 5 personas ──────────
        bool ofrecibleAparts = Ofrecible("aparts", cantidadPersonas);
        int apartDisponibles = !ofrecibleAparts ? 0 : new[] { "Apart 1", "Apart 2" }
            .Select(n => ByNombre(n))
            .Count(EsDisponible);

        // ── Cabaña chica (Cabaña 7) ───────────────────────────────────────────
        bool ofrecibleCabanaChica = Ofrecible("cabanaChica", cantidadPersonas);
        int cabChicaDisponibles = ofrecibleCabanaChica && EsDisponible(ByNombre("Cabaña 7")) ? 1 : 0;

        // ── Cabaña mediana (Cabañas 1-6) ──────────────────────────────────────
        bool ofrecibleCabanaMediana = Ofrecible("cabanaMediana", cantidadPersonas);
        var cabMedianaNombres = new[] {
            "Cabaña 1", "Cabaña 2", "Cabaña 3",
            "Cabaña 4", "Cabaña 5", "Cabaña 6"
        };
        int cabMedianaDisponibles = !ofrecibleCabanaMediana ? 0 : cabMedianaNombres
            .Select(n => ByNombre(n))
            .Count(EsDisponible);

        // ── Cabaña grande (Cabaña 8) ──────────────────────────────────────────
        bool ofrecibleCabanaGrande = Ofrecible("cabanaGrande", cantidadPersonas);
        int cabGrandeDisponibles = ofrecibleCabanaGrande && EsDisponible(ByNombre("Cabaña 8")) ? 1 : 0;

        // ── Casa (Icho Cruz) ───────────────────────────────────────────────────
        bool ofrecibleCasa = Ofrecible("casa", cantidadPersonas);
        var casa = alojamientos.FirstOrDefault(a => a.Tipo == TipoAlojamiento.Casa);
        int casaDisponibles = ofrecibleCasa && EsDisponible(casa) ? 1 : 0;

        // ── Libres por categoría (sin filtro de capacidad, para sugerir combinaciones) ──
        List<int> LibresDeCategoria(IEnumerable<string> nombres) =>
            nombres
                .Select(ByNombre)
                .Where(a => a is not null && !ocupados.Contains(a.Id))
                .Select(a => a!.Capacidad)
                .ToList();

        var libresHabitaciones = LibresDeCategoria(habNombres);
        var libresAparts = LibresDeCategoria(new[] { "Apart 1", "Apart 2" });
        var libresCabanaChica = LibresDeCategoria(new[] { "Cabaña 7" });
        var libresCabanaMediana = LibresDeCategoria(cabMedianaNombres);
        var libresCabanaGrande = LibresDeCategoria(new[] { "Cabaña 8" });
        var libresCasa = casa is not null && !ocupados.Contains(casa.Id)
            ? new List<int> { casa.Capacidad }
            : new List<int>();

        return Ok(new
        {
            habitaciones = new
            {
                disponible        = habDisponibles > 0,
                unidadesDisponibles = habDisponibles,
                ofrecible         = ofrecibleHabitaciones
            },
            aparts = new
            {
                disponible        = apartDisponibles > 0,
                unidadesDisponibles = apartDisponibles,
                ofrecible         = ofrecibleAparts
            },
            cabanaChica = new
            {
                disponible        = cabChicaDisponibles > 0,
                unidadesDisponibles = cabChicaDisponibles,
                ofrecible         = ofrecibleCabanaChica
            },
            cabanaMediana = new
            {
                disponible        = cabMedianaDisponibles > 0,
                unidadesDisponibles = cabMedianaDisponibles,
                ofrecible         = ofrecibleCabanaMediana
            },
            cabanaGrande = new
            {
                disponible        = cabGrandeDisponibles > 0,
                unidadesDisponibles = cabGrandeDisponibles,
                ofrecible         = ofrecibleCabanaGrande
            },
            casa = new
            {
                disponible        = casaDisponibles > 0,
                unidadesDisponibles = casaDisponibles,
                ofrecible         = ofrecibleCasa
            },
            libres = new
            {
                habitaciones  = libresHabitaciones,
                aparts        = libresAparts,
                cabanaChica   = libresCabanaChica,
                cabanaMediana = libresCabanaMediana,
                cabanaGrande  = libresCabanaGrande,
                casa          = libresCasa
            }
        });
    }
}
