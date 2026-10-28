namespace Ledgerly.MarketData;

public sealed record Quote(string Symbol, decimal Price, string Currency, DateTimeOffset AsOf);

/// <summary>
/// Deterministic fake prices: a symbol's price is a smooth function of (symbol, minute),
/// so two calls in the same minute agree and charts look plausible.
/// </summary>
public sealed class PriceGenerator(TimeProvider clock)
{
    public static readonly IReadOnlyList<string> KnownSymbols =
        ["AAPL", "MSFT", "NVDA", "ASML", "SAP", "NOVO", "VWCE", "IWDA", "EUNL", "BTC"];

    public Quote GetQuote(string symbol)
    {
        var normalized = symbol.Trim().ToUpperInvariant();
        var now = clock.GetUtcNow();
        var minute = now.ToUnixTimeSeconds() / 60;

        var seed = StableHash(normalized);
        var basePrice = 20m + (seed % 480);
        var wave = (decimal)Math.Sin((minute + seed) / 30.0) * 0.04m;
        var noise = ((StableHash($"{normalized}:{minute}") % 100) - 50) / 10_000m;

        var price = decimal.Round(basePrice * (1 + wave + noise), 2);
        return new Quote(normalized, price, "EUR", now);
    }

    private static int StableHash(string value)
    {
        // string.GetHashCode() is randomized per process in .NET. We need the same number on every replica.
        unchecked
        {
            var hash = 23;
            foreach (var c in value)
            {
                hash = (hash * 31) + c;
            }

            return Math.Abs(hash % 100_000);
        }
    }
}
