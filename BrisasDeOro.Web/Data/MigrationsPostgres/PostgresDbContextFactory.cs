using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BrisasDeOro.Web.Data.MigrationsPostgres;

public class PostgresDbContextFactory : IDesignTimeDbContextFactory<PostgresApplicationDbContext>
{
    public PostgresApplicationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connStr = config.GetConnectionString("DefaultConnection");

        var options = new DbContextOptionsBuilder<PostgresApplicationDbContext>()
            .UseNpgsql(connStr)
            .Options;

        return new PostgresApplicationDbContext(options);
    }
}
