// The whole Ledgerly system, described in C#. `aspire run` (or F5 on this project) starts everything.
var builder = DistributedApplication.CreateBuilder(args);

// ── Infrastructure (containers locally; real Azure services in the cloud, see infra/) ──────────
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent)   // keep the container (and data) between runs
    .WithDataVolume();
var db = sql.AddDatabase("ledgerlydb");

var cache = builder.AddRedis("cache");

var messaging = builder.AddAzureServiceBus("messaging").RunAsEmulator();
messaging.AddServiceBusQueue("imports");

// ── Services ────────────────────────────────────────────────────────────────────────────────────
var marketData = builder.AddProject<Projects.Ledgerly_MarketData>("marketdata")
    .WithHttpHealthCheck("/health");

var migrations = builder.AddProject<Projects.Ledgerly_MigrationService>("migrations")
    .WithReference(db)
    .WaitFor(db);

builder.AddProject<Projects.Ledgerly_Api>("api")
    .WithReference(db)
    .WithReference(cache)
    .WithReference(marketData)
    .WaitFor(cache)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Ledgerly_Worker>("worker")
    .WithReference(db)
    .WithReference(messaging)
    .WithReference(marketData)
    .WaitFor(messaging)
    .WaitForCompletion(migrations)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
