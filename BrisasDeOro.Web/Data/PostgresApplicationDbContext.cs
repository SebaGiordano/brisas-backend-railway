using Microsoft.EntityFrameworkCore;

namespace BrisasDeOro.Web.Data;

public class PostgresApplicationDbContext : ApplicationDbContext
{
    public PostgresApplicationDbContext(DbContextOptions<PostgresApplicationDbContext> options)
        : base(options) { }
}
