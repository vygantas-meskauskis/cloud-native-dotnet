using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ledgerly.Core.Data;

/// <summary>
/// Used only by the `dotnet ef` tool at design time (adding migrations, building bundles).
/// The connection string is a placeholder: no database is contacted when adding a migration.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LedgerlyDbContext>
{
    public LedgerlyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LedgerlyDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ledgerly-design;Trusted_Connection=True")
            .Options;

        return new LedgerlyDbContext(options);
    }
}
