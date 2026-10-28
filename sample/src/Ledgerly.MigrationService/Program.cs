using System.Diagnostics;
using Ledgerly.Core.Data;
using Ledgerly.Core.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<LedgerlyDbContext>("ledgerlydb");
builder.Services.Configure<AppDatabaseUserOptions>(builder.Configuration.GetSection(AppDatabaseUserOptions.Section));
builder.Services.AddHostedService<Migrator>();

builder.Build().Run();

/// <summary>
/// In Azure the migrator runs as the SQL admin identity, and the apps run as a separate, least-privileged identity.
/// When both values are set, the migrator makes sure that identity exists as a database user with read/write rights only.
/// </summary>
internal sealed class AppDatabaseUserOptions
{
    public const string Section = "Database:AppUser";

    public string? Name { get; set; }

    public string? ClientId { get; set; }
}

internal sealed partial class Migrator(
    IServiceProvider services,
    IHostApplicationLifetime lifetime,
    Microsoft.Extensions.Options.IOptions<AppDatabaseUserOptions> appUser,
    ILogger<Migrator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = LedgerlyTelemetry.ActivitySource.StartActivity("database migrate");
        try
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LedgerlyDbContext>();

            // The execution strategy retries transient failures (e.g. Azure SQL serverless waking up).
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () => await db.Database.MigrateAsync(stoppingToken));
            LogMigrated(logger);

            if (appUser.Value is { Name: { Length: > 0 } name, ClientId: { Length: > 0 } clientId })
            {
                await strategy.ExecuteAsync(async () => await EnsureAppUserAsync(db, name, Guid.Parse(clientId), stoppingToken));
                LogAppUserEnsured(logger, name);
            }
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            LogMigrationFailed(logger, ex);
            Environment.ExitCode = 1; // a failed job execution blocks the deployment pipeline
        }

        lifetime.StopApplication();
    }

    /// <summary>
    /// CREATE USER ... WITH SID, TYPE = E maps a managed identity (by its client id) without needing Microsoft Graph
    /// permissions on the SQL server. Idempotent, so it is safe on every deployment.
    /// </summary>
    private static async Task EnsureAppUserAsync(LedgerlyDbContext db, string name, Guid clientId, CancellationToken ct)
    {
        var user = name.Replace("]", "]]", StringComparison.Ordinal);
        var sid = "0x" + Convert.ToHexString(clientId.ToByteArray());
        var sql = $"""
            IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{user.Replace("'", "''", StringComparison.Ordinal)}')
                CREATE USER [{user}] WITH SID = {sid}, TYPE = E;
            ALTER ROLE db_datareader ADD MEMBER [{user}];
            ALTER ROLE db_datawriter ADD MEMBER [{user}];
            """;

#pragma warning disable EF1002 // values come from deployment configuration, are escaped above, and DDL cannot be parameterized
        await db.Database.ExecuteSqlRawAsync(sql, ct);
#pragma warning restore EF1002
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database migrated")]
    private static partial void LogMigrated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database user {UserName} ensured with read/write roles")]
    private static partial void LogAppUserEnsured(ILogger logger, string userName);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Database migration failed")]
    private static partial void LogMigrationFailed(ILogger logger, Exception ex);
}
