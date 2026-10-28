using Ledgerly.MarketData;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOutputCache();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PriceGenerator>();
builder.Services.AddSingleton(new ChaosState(builder.Configuration.GetSection("Chaos").Get<ChaosOptions>() ?? new ChaosOptions()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseOutputCache();

app.MapGet("/symbols", () => PriceGenerator.KnownSymbols)
    .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(10)));

app.MapGet("/prices", async (string symbols, PriceGenerator prices, ChaosState chaos, ILogger<Program> logger, CancellationToken ct) =>
{
    var settings = chaos.Current;

    if (settings.LatencyMs > 0)
    {
        await Task.Delay(settings.LatencyMs, ct);
    }

#pragma warning disable CA5394 // Random is fine for chaos injection, this is not security-sensitive.
    if (Random.Shared.NextDouble() < settings.FailureRate)
#pragma warning restore CA5394
    {
        logger.LogWarning("Chaos: failing /prices request for {Symbols}", symbols);
        return Results.Problem("Market data temporarily unavailable (chaos).", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var quotes = symbols
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(prices.GetQuote)
        .ToArray();

    return Results.Ok(quotes);
});

// Chaos controls. Only exposed in Development: never ship a "break me" button to production.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/chaos", (ChaosState chaos) => chaos.Current);
    app.MapPost("/chaos", (ChaosOptions options, ChaosState chaos) =>
    {
        chaos.Set(options);
        return Results.Ok(chaos.Current);
    });
}

app.MapDefaultEndpoints();

app.Run();
