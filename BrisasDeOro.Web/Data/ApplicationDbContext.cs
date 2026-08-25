using BrisasDeOro.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BrisasDeOro.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Alojamiento> Alojamientos => Set<Alojamiento>();
    public DbSet<Reserva>     Reservas      => Set<Reserva>();
    public DbSet<ApartDetalle> ApartDetalles => Set<ApartDetalle>();
    public DbSet<Pago>        Pagos         => Set<Pago>();
    public DbSet<Tarifa>      Tarifas       => Set<Tarifa>();
    public DbSet<Temporada>   Temporadas    => Set<Temporada>();
    public DbSet<ReservaAlojamiento> ReservaAlojamientos => Set<ReservaAlojamiento>();
    public DbSet<ReservaAireDia> ReservaAireDias => Set<ReservaAireDia>();
    public DbSet<GrupoVinculado> GruposVinculados => Set<GrupoVinculado>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Reserva>(e =>
        {
            e.Property(r => r.MontoTotal).HasPrecision(18, 2);
            e.Property(r => r.MontoSena).HasPrecision(18, 2);

            // Con "Npgsql.EnableLegacyTimestampBehavior" activo (necesario para que las
            // comparaciones con DateTime.Today/Now de Kind=Unspecified no exploten contra
            // columnas "timestamp with time zone"), Npgsql convierte automáticamente estas
            // fechas a la hora local del proceso al LEERLAS. En horario Argentina (UTC-3)
            // eso retrocede un día las fechas guardadas a medianoche UTC (ej. 31/12 → 30/12).
            // Este converter deshace esa conversión al leer, devolviendo la fecha original.
            // HasColumnType explícito evita que `dotnet ef migrations add` recalcule el tipo
            // de columna con el mapeo por defecto de la versión actual de Npgsql (que no
            // coincide con el tipo real ya creado en la base) y genere un ALTER COLUMN falso.
            e.Property(r => r.FechaIngreso).HasColumnType("timestamp with time zone").HasConversion(FechaSinConversionLocal);
            e.Property(r => r.FechaSalida).HasColumnType("timestamp with time zone").HasConversion(FechaSinConversionLocal);
            e.Property(r => r.FechaCarga).HasColumnType("timestamp with time zone");
        });

        builder.Entity<Pago>(e =>
        {
            e.Property(p => p.Monto).HasPrecision(18, 2);
            e.Property(p => p.Fecha).HasColumnType("timestamp with time zone");
        });

        builder.Entity<Temporada>(e =>
        {
            e.Property(t => t.FechaInicio).HasColumnType("timestamp with time zone");
            e.Property(t => t.FechaFin).HasColumnType("timestamp with time zone");
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FechaCreacion).HasColumnType("timestamp with time zone");
        });

        builder.Entity<ReservaAlojamiento>(e =>
        {
            e.HasOne(ra => ra.Reserva)
             .WithMany(r => r.UnidadesGrupales)
             .HasForeignKey(ra => ra.ReservaId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ra => ra.Alojamiento)
             .WithMany()
             .HasForeignKey(ra => ra.AlojamientoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ReservaAireDia>(e =>
        {
            e.HasOne(a => a.Reserva)
             .WithMany()
             .HasForeignKey(a => a.ReservaId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Alojamiento)
             .WithMany()
             .HasForeignKey(a => a.AlojamientoId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(a => new { a.ReservaId, a.AlojamientoId, a.Fecha }).IsUnique();

            // Mismo problema y misma solución que Reserva.FechaIngreso/FechaSalida (ver
            // comentario en ese bloque): con Npgsql en modo legacy timestamp, la lectura
            // convierte automáticamente a hora local, corriendo la fecha un día hacia atrás
            // en Argentina. Este converter deshace esa conversión al leer.
            e.Property(a => a.Fecha)
             .HasColumnType("timestamp with time zone")
             .HasConversion(FechaSinConversionLocal);
        });

        builder.Entity<GrupoVinculado>(e =>
        {
            e.HasMany(g => g.Reservas)
             .WithOne(r => r.GrupoVinculado)
             .HasForeignKey(r => r.GrupoVinculadoId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ApartDetalle>(e =>
        {
            e.HasOne(a => a.AlojamientoApart)
             .WithMany()
             .HasForeignKey(a => a.AlojamientoApartId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.AlojamientoHab1)
             .WithMany()
             .HasForeignKey(a => a.AlojamientoHab1Id)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.AlojamientoHab2)
             .WithMany()
             .HasForeignKey(a => a.AlojamientoHab2Id)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Tarifa>(e =>
        {
            e.Property(t => t.PrecioConDesayuno).HasPrecision(18, 2);
            e.Property(t => t.PrecioSinDesayuno).HasPrecision(18, 2);
            e.HasOne(t => t.Alojamiento)
             .WithMany()
             .HasForeignKey(t => t.AlojamientoId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => new { t.AlojamientoId, t.CantidadPersonas, t.TemporadaId }).IsUnique();
            e.HasOne(t => t.Temporada)
             .WithMany()
             .HasForeignKey(t => t.TemporadaId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // Al escribir no hace falta tocar nada (Npgsql en modo legacy guarda el valor tal
    // cual, sin conversión). Al leer, si Npgsql devolvió la fecha ya convertida a hora
    // local (Kind=Local), se la vuelve a pasar a UTC para recuperar el valor original.
    private static readonly ValueConverter<DateTime, DateTime> FechaSinConversionLocal = new(
        v => v,
        v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v);
}
