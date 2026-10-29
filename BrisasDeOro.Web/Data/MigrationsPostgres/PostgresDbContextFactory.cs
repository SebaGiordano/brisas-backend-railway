using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BrisasDeOro.Web.Data.MigrationsPostgres;

public class PostgresDbContextFactory : IDesignTimeDbContextFactory<PostgresApplicationDbContext>
{
    public PostgresApplicationDbContext CreateDbContext(string[] args)
    {
        // Mantiene paridad con el switch que Program.cs activa en runtime (ver comentario en
        // ApplicationDbContext.OnModelCreating). No afecta el mapeo de tipos de columna que usa
        // el scaffolding de EF (eso depende de la versión del paquete Npgsql), solo el
        // comportamiento de comparación/conversión de DateTime.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddUserSecrets(typeof(PostgresDbContextFactory).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Misma conversión que Program.cs: en Railway se usa DATABASE_URL; en desarrollo
        // local, ConnectionStrings:DefaultConnection (hoy en User Secrets).
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        string? connStr;
        if (databaseUrl != null)
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            connStr = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
        }
        else
        {
            connStr = config.GetConnectionString("DefaultConnection");
        }

        var options = new DbContextOptionsBuilder<PostgresApplicationDbContext>()
            .UseNpgsql(connStr)
            .Options;

        return new PostgresApplicationDbContext(options);
    }
}
