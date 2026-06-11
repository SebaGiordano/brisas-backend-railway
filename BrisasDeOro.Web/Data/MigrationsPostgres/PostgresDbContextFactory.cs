using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BrisasDeOro.Web.Data.MigrationsPostgres;

public class PostgresDbContextFactory : IDesignTimeDbContextFactory<PostgresApplicationDbContext>
{
    public PostgresApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PostgresApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=brisas;Username=postgres;Password=postgres")
            .Options;
        return new PostgresApplicationDbContext(options);
    }
}
