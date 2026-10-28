namespace Ledgerly.Core.Domain;

public enum ImportStatus
{
    Pending,
    Completed,
    Failed,
}

/// <summary>A broker CSV file waiting to be (or already) processed by the worker.</summary>
public sealed class Import
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid PortfolioId { get; init; }
    public required string OwnerId { get; init; }
    public required string FileName { get; init; }

    // Kept in the database to keep the sample small. In production put the file in Blob Storage and store the URI.
    public required string Content { get; init; }

    public ImportStatus Status { get; set; } = ImportStatus.Pending;
    public int RowsImported { get; set; }
    public int RowsSkipped { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}

public enum AlertDirection
{
    Above,
    Below,
}

public sealed class PriceAlert
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string OwnerId { get; init; }
    public required string Symbol { get; init; }
    public decimal TargetPrice { get; init; }
    public AlertDirection Direction { get; init; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? TriggeredAt { get; set; }

    public bool IsTriggeredBy(decimal price) =>
        Direction == AlertDirection.Above ? price >= TargetPrice : price <= TargetPrice;
}

public sealed class Notification
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string OwnerId { get; init; }
    public required string Message { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
