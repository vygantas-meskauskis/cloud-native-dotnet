using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Ledgerly.Api;
using Ledgerly.Api.Endpoints;
using Ledgerly.Api.Services;
using Ledgerly.Core.Data;
using Ledgerly.Core.Market;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.FeatureManagement;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Cross-cutting: OpenTelemetry, health checks, resilience, service discovery ────────────────
builder.AddServiceDefaults();

// ── Data: Aspire client integration = DbContext pooling + retries + health check + tracing ──────
builder.AddSqlServerDbContext<LedgerlyDbContext>("ledgerlydb");

// ── Caching: HybridCache (in-memory L1). Redis becomes the shared L2 when a "cache" connection exists.
if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("cache")))
{
    builder.AddRedisDistributedCache("cache");
}

builder.Services.AddHybridCache(options => options.DefaultEntryOptions = new HybridCacheEntryOptions
{
    Expiration = TimeSpan.FromSeconds(30),
    LocalCacheExpiration = TimeSpan.FromSeconds(10),
});

// ── Outbound HTTP: typed client. Resilience + discovery come from ConfigureHttpClientDefaults. ───
builder.Services.AddHttpClient<IMarketDataClient, MarketDataClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["MarketData:BaseUrl"] ?? "https+http://marketdata"));
builder.Services.AddScoped<ValuationService>();
builder.Services.AddSingleton(TimeProvider.System);

// ── Security: JWT bearer. Settings come from "Authentication:Schemes:Bearer" ───────────────────
// Locally: `dotnet user-jwts create`. In Azure: Authority = your Entra ID tenant, audience = the API app.
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

// ── API surface: validation, ProblemDetails, OpenAPI, JSON ─────────────────────────────────────
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ── Protection: per-user rate limit for the expensive import endpoint ──────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(ApiPolicies.Imports, http => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: http.User.Identity?.Name ?? http.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
});

// ── Feature flags (from configuration now, Azure App Configuration later) ──────────────────────
builder.Services.AddFeatureManagement();

// ── Ready for a React SPA later: allowed origins come from configuration ────────────────────────
var spaOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy(ApiPolicies.Spa, p => p.WithOrigins(spaOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(ApiPolicies.Spa);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                 // /openapi/v1.json
    app.MapScalarApiReference();      // /scalar  (interactive API explorer)
}

app.MapPortfolioEndpoints();
app.MapImportEndpoints();
app.MapAlertEndpoints();
app.MapDefaultEndpoints();

app.Run();

// Makes the auto-generated Program class visible to WebApplicationFactory<Program> in integration tests.
public partial class Program;
