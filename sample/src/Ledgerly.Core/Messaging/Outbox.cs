using System.Text.Json;
using Ledgerly.Core.Data;

namespace Ledgerly.Core.Messaging;

/// <summary>
/// Transactional outbox row. Written in the SAME database transaction as the business change,
/// then relayed to Service Bus by the worker. No "saved to DB but message lost" gap.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Type { get; init; }
    public required string Destination { get; init; }
    public required string Payload { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }

    // W3C trace context of the request that created the message, so the trace continues in the worker.
    public string? TraceParent { get; init; }
}

/// <summary>Idempotency record: "this handler already processed this message id".</summary>
public sealed class ProcessedMessage
{
    public required string MessageId { get; init; }
    public required string Handler { get; init; }
    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;
}

public static class Queues
{
    public const string Imports = "imports";
}

public sealed record ImportRequested(Guid ImportId, Guid PortfolioId);

public static class OutboxExtensions
{
    /// <summary>Adds a message to the outbox. It is saved by the caller's next SaveChangesAsync.</summary>
    public static void EnqueueOutboxMessage<T>(this LedgerlyDbContext db, string destination, T message)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(T).Name,
            Destination = destination,
            Payload = JsonSerializer.Serialize(message),
            TraceParent = System.Diagnostics.Activity.Current?.Id,
        });
    }
}
