using BrisasDeOro.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BrisasDeOro.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager  = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager  = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context      = services.GetRequiredService<ApplicationDbContext>();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
        await SeedAlojamientosAsync(context);
        await SeedTemporadasAsync(context);
        await SeedTarifasAsync(context);
    }

    // ── Roles ────────────────────────────────────────────────────────────────

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { "Administrador", "Viewer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // ── Usuario administrador inicial ─────────────────────────────────────────

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        const string userName = "Sebastian";
        const string password = "Admin1234!";
        const string email    = "brisasdeoro@gmail.com";

        if (await userManager.FindByNameAsync(userName) != null)
            return;

        var admin = new ApplicationUser
        {
            UserName       = userName,
            Email          = email,
            EmailConfirmed = true,
            FechaCreacion  = DateTime.Now
        };

        var result = await userManager.CreateAsync(admin, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Administrador");
    }

    // ── Alojamientos ──────────────────────────────────────────────────────────

    private static async Task SeedAlojamientosAsync(ApplicationDbContext context)
    {
        var existingNames = (await context.Alojamientos
            .Select(a => a.Nombre)
            .ToListAsync())
            .ToHashSet();

        var toAdd = new List<Alojamiento>();

        var habitaciones = new (string Nombre, int Capacidad)[]
        {
            ("Habitación 4",  3),
            ("Habitación 5",  4),
            ("Habitación 6",  2),
            ("Habitación 7",  4),
            ("Habitación 8",  3),
            ("Habitación 9",  3),
            ("Habitación 10", 2),
            ("Habitación 11", 3),
        };
        foreach (var (nombre, cap) in habitaciones)
            if (!existingNames.Contains(nombre))
                toAdd.Add(new Alojamiento { Nombre = nombre, Tipo = TipoAlojamiento.Habitacion, Capacidad = cap });

        var cabanas = new (string Nombre, int Capacidad)[]
        {
            ("Cabaña 1", 6),
            ("Cabaña 2", 6),
            ("Cabaña 3", 6),
            ("Cabaña 4", 6),
            ("Cabaña 5", 6),
            ("Cabaña 6", 6),
            ("Cabaña 7", 3),
            ("Cabaña 8", 8),
        };
        foreach (var (nombre, cap) in cabanas)
            if (!existingNames.Contains(nombre))
                toAdd.Add(new Alojamiento { Nombre = nombre, Tipo = TipoAlojamiento.Cabaña, Capacidad = cap });

        var aparts = new (string Nombre, int Capacidad)[]
        {
            ("Apart 1", 5),
            ("Apart 2", 5),
        };
        foreach (var (nombre, cap) in aparts)
            if (!existingNames.Contains(nombre))
                toAdd.Add(new Alojamiento { Nombre = nombre, Tipo = TipoAlojamiento.Apart, Capacidad = cap });

        if (toAdd.Count > 0)
        {
            context.Alojamientos.AddRange(toAdd);
            await context.SaveChangesAsync();
        }

        await SeedApartDetallesAsync(context);
    }

    // ── Temporadas ────────────────────────────────────────────────────────────

    private static async Task SeedTemporadasAsync(ApplicationDbContext context)
    {
        var nombres = (await context.Temporadas.Select(t => t.Nombre).ToListAsync()).ToHashSet();

        var toAdd = new List<Temporada>();

        if (!nombres.Contains("Temporada Alta"))
            toAdd.Add(new Temporada
            {
                Nombre      = "Temporada Alta",
                FechaInicio = new DateTime(2025, 12, 24),
                FechaFin    = new DateTime(2026,  4, 19),
                Activo      = true
            });

        if (!nombres.Contains("Temporada Baja"))
            toAdd.Add(new Temporada
            {
                Nombre      = "Temporada Baja",
                FechaInicio = new DateTime(2026,  4, 20),
                FechaFin    = new DateTime(2026, 12, 23),
                Activo      = true
            });

        if (toAdd.Count > 0)
        {
            context.Temporadas.AddRange(toAdd);
            await context.SaveChangesAsync();
        }
    }

    // ── Tarifas ───────────────────────────────────────────────────────────────

    private static async Task SeedTarifasAsync(ApplicationDbContext context)
    {
        var temporadas = await context.Temporadas.ToListAsync();
        if (!temporadas.Any()) return;

        var alojamientos = await context.Alojamientos.ToListAsync();

        // Eliminar tarifas de Apart con menos de 3 personas
        var apartIds = alojamientos
            .Where(a => a.Tipo == TipoAlojamiento.Apart)
            .Select(a => a.Id)
            .ToList();

        var incorrectas = await context.Tarifas
            .Where(t => apartIds.Contains(t.AlojamientoId) && t.CantidadPersonas < 3)
            .ToListAsync();

        if (incorrectas.Count > 0)
        {
            context.Tarifas.RemoveRange(incorrectas);
            await context.SaveChangesAsync();
        }

        // Crear tarifas faltantes por temporada
        foreach (var temporada in temporadas)
        {
            var existentes = (await context.Tarifas
                .Where(t => t.TemporadaId == temporada.Id)
                .Select(t => new { t.AlojamientoId, t.CantidadPersonas })
                .ToListAsync())
                .Select(t => (t.AlojamientoId, t.CantidadPersonas))
                .ToHashSet();

            var nuevas = new List<Tarifa>();
            foreach (var a in alojamientos)
            {
                int inicio = a.Tipo == TipoAlojamiento.Apart ? 3 : 1;
                for (int p = inicio; p <= a.Capacidad; p++)
                {
                    if (!existentes.Contains((a.Id, p)))
                    {
                        nuevas.Add(new Tarifa
                        {
                            AlojamientoId     = a.Id,
                            TemporadaId       = temporada.Id,
                            CantidadPersonas  = p,
                            PrecioConDesayuno = 0,
                            PrecioSinDesayuno = 0,
                            Activo            = true
                        });
                    }
                }
            }

            if (nuevas.Count > 0)
            {
                context.Tarifas.AddRange(nuevas);
                await context.SaveChangesAsync();
            }
        }
    }

    private static async Task SeedApartDetallesAsync(ApplicationDbContext context)
    {
        var alojamientos = await context.Alojamientos
            .Where(a => a.Nombre == "Apart 1"      || a.Nombre == "Apart 2" ||
                        a.Nombre == "Habitación 6"  || a.Nombre == "Habitación 8" ||
                        a.Nombre == "Habitación 10" || a.Nombre == "Habitación 11")
            .ToDictionaryAsync(a => a.Nombre, a => a.Id);

        var toAdd = new List<ApartDetalle>();

        if (alojamientos.TryGetValue("Apart 1", out var apart1Id) &&
            !await context.ApartDetalles.AnyAsync(d => d.AlojamientoApartId == apart1Id))
        {
            toAdd.Add(new ApartDetalle
            {
                AlojamientoApartId = apart1Id,
                AlojamientoHab1Id  = alojamientos["Habitación 6"],
                AlojamientoHab2Id  = alojamientos["Habitación 8"],
            });
        }

        if (alojamientos.TryGetValue("Apart 2", out var apart2Id) &&
            !await context.ApartDetalles.AnyAsync(d => d.AlojamientoApartId == apart2Id))
        {
            toAdd.Add(new ApartDetalle
            {
                AlojamientoApartId = apart2Id,
                AlojamientoHab1Id  = alojamientos["Habitación 10"],
                AlojamientoHab2Id  = alojamientos["Habitación 11"],
            });
        }

        if (toAdd.Count > 0)
        {
            context.ApartDetalles.AddRange(toAdd);
            await context.SaveChangesAsync();
        }
    }
}
