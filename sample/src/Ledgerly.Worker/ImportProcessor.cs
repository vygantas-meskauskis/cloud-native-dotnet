using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Ledgerly.Core.Data;
using Ledgerly.Core.Diagnostics;
using Ledgerly.Core.Domain;
using Ledgerly.Core.Imports;
using Ledgerly.Core.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Worker;

/// <summary>
/// Consumes "imports" from Service Bus. Idempotent: a message id is processed at most once
/// (ProcessedMessages table, primary key = message id). Failures are abandoned and retried by
/// Service Bus; after MaxDeliveryCount attempts the message lands in the dead-letter queue.
/// </summary>
public sealed partial class ImportProcessor(
    ServiceBusClient serviceBus, IServiceScopeFactory scopes, TimeProvider clock, ILogger<ImportProcessor> logger) : BackgroundService
{
    private const string HandlerName = nameof(ImportProcessor);
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = serviceBus.CreateProcessor(Queues.Imports, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 4,
            AutoCompleteMessages = false,
        });
        _processor.ProcessMessageAsync += HandleAsync;
        _processor.ProcessErrorAsync += args =>
        {
            LogProcessorError(logger, args.ErrorSource.ToString(), args.Exception);
            return Task.CompletedTask;
        };

        await _processor.StartProcessingAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Graceful shutdown: stop taking new messages, let in-flight handlers finish.
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task HandleAsync(ProcessMessageEventArgs args)
    {
        var messageId = args.Message.MessageId;
        var ct = args.CancellationToken;
        var started = Stopwatch.GetTimestamp();

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LedgerlyDbContext>();

        if (await db.ProcessedMessages.AnyAsync(p => p.MessageId == messageId, ct))
        {
            LogDuplicate(logger, messageId);
            await args.CompleteMessageAsync(args.Message, ct);
            return;
        }

        var request = args.Message.Body.ToObjectFromJson<ImportRequested>();
        using var activity = LedgerlyTelemetry.ActivitySource.StartActivity("import process");
        activity?.SetTag("ledgerly.import.id", request?.ImportId);

        var import = request is null ? null : await db.Imports.SingleOrDefaultAsync(i => i.Id == request.ImportId, ct);
        if (import is null)
        {
            // Poison message: retrying will never help. Park it in the DLQ for a human to look at.
            await args.DeadLetterMessageAsync(args.Message, "ImportNotFound", $"No import for message {messageId}", ct);
            return;
        }

        try
        {
            var outcome = await ApplyImportAsync(db, import, ct);
            db.ProcessedMessages.Add(new ProcessedMessage { MessageId = messageId, Handler = HandlerName });
            await db.SaveChangesAsync(ct); // trades + import status + idempotency record in ONE transaction
            await args.CompleteMessageAsync(args.Message, ct);

            LedgerlyTelemetry.ImportsProcessed.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
            LedgerlyTelemetry.ImportDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds);
            LogImportDone(logger, import.Id, outcome, import.RowsImported, import.RowsSkipped);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            LogImportCrashed(logger, import.Id, args.Message.DeliveryCount, ex);
            await args.AbandonMessageAsync(args.Message, cancellationToken: ct); // Service Bus will redeliver
        }
    }

    private async Task<string> ApplyImportAsync(LedgerlyDbContext db, Import import, CancellationToken ct)
    {
        var parsed = BrokerCsvParser.Parse(import.Content);
        var portfolio = await db.Portfolios.Include(p => p.Transactions).SingleAsync(p => p.Id == import.PortfolioId, ct);

        var knownRefs = portfolio.Transactions
            .Where(t => t.ExternalRef is not null)
            .Select(t => t.ExternalRef!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newTrades = parsed.Trades
            .Where(t => knownRefs.Add(t.Reference)) // skips rows already imported (and duplicates inside the file)
            .Select(t => new Transaction
            {
                PortfolioId = portfolio.Id,
                Symbol = t.Symbol,
                Type = t.Type,
                Quantity = t.Quantity,
                Price = t.Price,
                TradeDate = t.TradeDate,
                ExternalRef = t.Reference,
                ImportId = import.Id,
            })
            .ToList();

        import.CompletedAt = clock.GetUtcNow();
        import.RowsSkipped = parsed.Trades.Count - newTrades.Count + parsed.Errors.Count;

        try
        {
            HoldingCalculator.Calculate([.. portfolio.Transactions, .. newTrades]);
        }
        catch (HoldingsException ex)
        {
            import.Status = ImportStatus.Failed;
            import.Error = ex.Message;
            return "rejected";
        }

        // Add through the DbSet, not portfolio.Transactions: see the note in PortfolioEndpoints.RecordTransaction.
        db.Transactions.AddRange(newTrades);
        portfolio.UpdatedAt = clock.GetUtcNow();
        import.RowsImported = newTrades.Count;
        import.Status = ImportStatus.Completed;
        import.Error = parsed.Errors.Count == 0
            ? null
            : string.Join("; ", parsed.Errors.Take(5).Select(e => $"line {e.Line}: {e.Message}"));

        LedgerlyTelemetry.TransactionsRecorded.Add(newTrades.Count, new KeyValuePair<string, object?>("source", "import"));
        return parsed.Errors.Count == 0 ? "completed" : "completed_with_errors";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Message {MessageId} already processed, completing duplicate")]
    private static partial void LogDuplicate(ILogger logger, string messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Import {ImportId} {Outcome}: {RowsImported} imported, {RowsSkipped} skipped")]
    private static partial void LogImportDone(ILogger logger, Guid importId, string outcome, int rowsImported, int rowsSkipped);

    [LoggerMessage(Level = LogLevel.Error, Message = "Import {ImportId} failed on delivery {DeliveryCount}; abandoning for retry")]
    private static partial void LogImportCrashed(ILogger logger, Guid importId, int deliveryCount, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Service Bus processor error from {Source}")]
    private static partial void LogProcessorError(ILogger logger, string source, Exception ex);
}
