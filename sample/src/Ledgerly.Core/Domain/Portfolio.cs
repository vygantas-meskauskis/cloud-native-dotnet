namespace Ledgerly.Core.Domain;

public sealed class Portfolio
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string OwnerId { get; init; }
    public required string Name { get; set; }
    public string BaseCurrency { get; init; } = "EUR";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // Touched on every change (including new trades) so the rowversion check below protects the whole aggregate.
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // SQL Server rowversion: EF Core uses it for optimistic concurrency.
    public byte[] RowVersion { get; set; } = [];

    public List<Transaction> Transactions { get; init; } = [];
}

public enum TransactionType
{
    Buy,
    Sell,
}

public sealed class Transaction
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid PortfolioId { get; init; }
    public required string Symbol { get; init; }
    public TransactionType Type { get; init; }
    public decimal Quantity { get; init; }
    public decimal Price { get; init; }
    public DateOnly TradeDate { get; init; }

    // Broker reference for imported rows. Unique per portfolio, so a re-imported file cannot double-count.
    public string? ExternalRef { get; init; }
    public Guid? ImportId { get; init; }
}
