using BrisasDeOro.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BrisasDeOro.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Alojamiento> Alojamientos => Set<Alojamiento>();
    public DbSet<Reserva>     Reservas      => Set<Reserva>();
    public DbSet<ApartDetalle> ApartDetalles => Set<ApartDetalle>();
    public DbSet<Pago>        Pagos         => Set<Pago>();
    public DbSet<Tarifa>      Tarifas       => Set<Tarifa>();
    public DbSet<Temporada>   Temporadas    => Set<Temporada>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Reserva>(e =>
        {
            e.Property(r => r.MontoTotal).HasPrecision(18, 2);
            e.Property(r => r.MontoSena).HasPrecision(18, 2);
        });

        builder.Entity<Pago>(e =>
        {
            e.Property(p => p.Monto).HasPrecision(18, 2);
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
}
