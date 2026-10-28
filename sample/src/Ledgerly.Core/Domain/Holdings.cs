namespace Ledgerly.Core.Domain;

public sealed record Holding(string Symbol, decimal Quantity, decimal AverageCost)
{
    public decimal CostBasis => Quantity * AverageCost;
}

public sealed class HoldingsException(string message) : Exception(message);

/// <summary>Pure domain logic: turns a list of trades into current holdings (average-cost method).</summary>
public static class HoldingCalculator
{
    public static IReadOnlyList<Holding> Calculate(IEnumerable<Transaction> transactions)
    {
        var positions = new Dictionary<string, (decimal Quantity, decimal Cost)>(StringComparer.OrdinalIgnoreCase);

        foreach (var tx in transactions.OrderBy(t => t.TradeDate).ThenBy(t => t.Id))
        {
            positions.TryGetValue(tx.Symbol, out var pos);

            if (tx.Type == TransactionType.Buy)
            {
                pos = (pos.Quantity + tx.Quantity, pos.Cost + (tx.Quantity * tx.Price));
            }
            else
            {
                if (tx.Quantity > pos.Quantity)
                {
                    throw new HoldingsException($"Cannot sell {tx.Quantity} {tx.Symbol}: only {pos.Quantity} held on {tx.TradeDate:yyyy-MM-dd}.");
                }

                var averageCost = pos.Quantity == 0 ? 0 : pos.Cost / pos.Quantity;
                pos = (pos.Quantity - tx.Quantity, pos.Cost - (tx.Quantity * averageCost));
            }

            positions[tx.Symbol] = pos;
        }

        return [.. positions
            .Where(p => p.Value.Quantity > 0)
            .Select(p => new Holding(p.Key.ToUpperInvariant(), p.Value.Quantity, decimal.Round(p.Value.Cost / p.Value.Quantity, 4)))
            .OrderBy(h => h.Symbol, StringComparer.Ordinal)];
    }
}
