using Ledgerly.Core.Data;
using Ledgerly.Core.Market;
using Ledgerly.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<LedgerlyDbContext>("ledgerlydb");
builder.AddAzureServiceBusClient("messaging");

builder.Services.AddHttpClient<IMarketDataClient, MarketDataClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["MarketData:BaseUrl"] ?? "https+http://marketdata"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<AlertOptions>(builder.Configuration.GetSection("Alerts"));

builder.Services.AddHostedService<OutboxRelay>();
builder.Services.AddHostedService<ImportProcessor>();
builder.Services.AddHostedService<AlertEvaluator>();

// Give in-flight messages time to finish when Container Apps stops a replica (SIGTERM).
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(25));

var app = builder.Build();
app.MapDefaultEndpoints();
app.Run();
