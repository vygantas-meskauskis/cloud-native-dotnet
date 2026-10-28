using System.Collections.Concurrent;
using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Ledgerly.Core.Data;
using Ledgerly.Core.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Worker;

/// <summary>
/// Transactional outbox relay: reads unsent rows from OutboxMessages and publishes them to Service Bus.
/// Delivery is at-least-once (a crash after Send but before SaveChanges re-sends), so consumers must be idempotent.
/// The outbox row id becomes the Service Bus MessageId, which lets duplicate detection and our
/// ProcessedMessages table recognise a re-send.
/// </summary>
public sealed partial class OutboxRelay(
    IServiceScopeFactory scopes, ServiceBusClient serviceBus, TimeProvider clock, ILogger<OutboxRelay> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 10;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2), clock);
        do
        {
            try
            {
                await RelayBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogRelayFailed(logger, ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RelayBatchAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LedgerlyDbContext>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var message in pending)
        {
            // Continue the trace of the HTTP request that created the message.
            using var activity = LedgerlyTelemetry.ActivitySource.StartActivity(
                $"outbox publish {message.Destination}", ActivityKind.Producer, message.TraceParent);

            try
            {
                var sender = _senders.GetOrAdd(message.Destination, serviceBus.CreateSender);
                await sender.SendMessageAsync(
                    new ServiceBusMessage(message.Payload)
                    {
                        MessageId = message.Id.ToString(),
                        Subject = message.Type,
                        ContentType = "application/json",
                    },
                    ct);

                message.ProcessedAt = clock.GetUtcNow();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.Attempts++;
                message.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                activity?.AddException(ex);
                activity?.SetStatus(ActivityStatusCode.Error);
                LogPublishFailed(logger, message.Id, message.Attempts, ex);
            }
        }

        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox relay batch failed")]
    private static partial void LogRelayFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Publishing outbox message {MessageId} failed (attempt {Attempt})")]
    private static partial void LogPublishFailed(ILogger logger, Guid messageId, int attempt, Exception ex);
}
