using Ledgerly.Core.Data;
using Ledgerly.Core.Diagnostics;
using Ledgerly.Core.Domain;
using Ledgerly.Core.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ledgerly.Worker;

public sealed class AlertOptions
{
    public int IntervalSeconds { get; set; } = 30;
}

/// <summary>
/// Periodically checks active price alerts against live quotes.
/// Run the worker with ONE replica (or move this to a scheduled Container Apps job) so alerts are not evaluated twice.
/// </summary>
public sealed partial class AlertEvaluator(
    IServiceScopeFactory scopes, IMarketDataClient marketData, IOptions<AlertOptions> options, TimeProvider clock,
    ILogger<AlertEvaluator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervalSeconds), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await EvaluateAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One bad tick (market data down, DB failover) must not kill the loop.
                LogEvaluationFailed(logger, ex);
            }
        }
    }

    private async Task EvaluateAsync(CancellationToken ct)
    {
        using var activity = LedgerlyTelemetry.ActivitySource.StartActivity("alerts evaluate");
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LedgerlyDbContext>();

        var alerts = await db.PriceAlerts.Where(a => a.IsActive).ToListAsync(ct);
        activity?.SetTag("ledgerly.alerts.active", alerts.Count);
        if (alerts.Count == 0)
        {
            return;
        }

        var quotes = await marketData.GetQuotesAsync([.. alerts.Select(a => a.Symbol).Distinct()], ct);

        foreach (var alert in alerts)
        {
            if (quotes.TryGetValue(alert.Symbol, out var quote) && alert.IsTriggeredBy(quote.Price))
            {
                alert.IsActive = false;
                alert.TriggeredAt = clock.GetUtcNow();
                db.Notifications.Add(new Notification
                {
                    OwnerId = alert.OwnerId,
                    Message = $"{alert.Symbol} is {quote.Price} {quote.Currency} ({alert.Direction.ToString().ToLowerInvariant()} your target {alert.TargetPrice}).",
                });
                LedgerlyTelemetry.AlertsTriggered.Add(1, new KeyValuePair<string, object?>("symbol", alert.Symbol));
                LogAlertTriggered(logger, alert.Id, alert.Symbol, quote.Price);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Alert {AlertId} triggered for {Symbol} at {Price}")]
    private static partial void LogAlertTriggered(ILogger logger, Guid alertId, string symbol, decimal price);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Alert evaluation failed; will retry on next tick")]
    private static partial void LogEvaluationFailed(ILogger logger, Exception ex);
}
