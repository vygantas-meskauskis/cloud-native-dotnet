using System.Security.Claims;
using System.Text.Encodings.Web;
using Ledgerly.Core.Data;
using Ledgerly.Core.Market;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.MsSql;
using Xunit;

namespace Ledgerly.IntegrationTests;

/// <summary>
/// Runs the real API in memory against a REAL SQL Server (not the EF in-memory provider).
/// Where the database comes from:
///   1. LEDGERLY_TEST_SQL env var (e.g. LocalDB on a Windows dev box), or
///   2. a throwaway SQL Server container via Testcontainers (CI runners have Docker).
/// If neither is available the tests are skipped, not failed.
/// </summary>
public sealed class LedgerlyApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private MsSqlContainer? _container;
    private string? _connectionString;

    public string? SkipReason { get; private set; }

    public FakeMarketData MarketData { get; } = new();

    public async ValueTask InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable("LEDGERLY_TEST_SQL");
        if (!string.IsNullOrWhiteSpace(external))
        {
            var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(external)
            {
                InitialCatalog = $"LedgerlyTests_{Guid.NewGuid():N}",
            };
            _connectionString = csb.ConnectionString;
        }
        else
        {
            try
            {
                _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
                await _container.StartAsync();
                _connectionString = _container.GetConnectionString();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                SkipReason = $"No SQL Server available (set LEDGERLY_TEST_SQL or start Docker): {ex.GetType().Name}";
                return;
            }
        }

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LedgerlyDbContext>().Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting is applied before Program.cs reads configuration, so Aspire's client integration sees it.
        builder.UseSetting("ConnectionStrings:ledgerlydb", _connectionString ?? "Server=unused;Database=unused");

        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMarketDataClient>();
            services.AddSingleton<IMarketDataClient>(MarketData);

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>An HttpClient that is "logged in" as the given user.</summary>
    public HttpClient CreateClientFor(string userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userId);
        return client;
    }

    public override async ValueTask DisposeAsync()
    {
        if (_connectionString is not null && _container is null && SkipReason is null)
        {
            await using var scope = Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<LedgerlyDbContext>().Database.EnsureDeletedAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}

/// <summary>Authenticates whoever is named in the X-Test-User header. Test code only!</summary>
internal sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserHeader = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out var user) || string.IsNullOrEmpty(user))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity([new Claim("sub", user!), new Claim(ClaimTypes.Name, user!)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

/// <summary>Controllable stand-in for the market-data service.</summary>
public sealed class FakeMarketData : IMarketDataClient
{
    public Dictionary<string, decimal> Prices { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsDown { get; set; }

    public Task<IReadOnlyDictionary<string, Quote>> GetQuotesAsync(IReadOnlyCollection<string> symbols, CancellationToken cancellationToken)
    {
        if (IsDown)
        {
            throw new HttpRequestException("market data is down (fake)");
        }

        IReadOnlyDictionary<string, Quote> result = symbols
            .Where(Prices.ContainsKey)
            .ToDictionary(s => s, s => new Quote(s, Prices[s], "EUR", DateTimeOffset.UtcNow), StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(result);
    }
}
