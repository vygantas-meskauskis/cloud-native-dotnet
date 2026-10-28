using Ledgerly.Core.Data;
using Ledgerly.Core.Domain;
using Ledgerly.Core.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ledgerly.Api.Services;

public sealed record ValuationLine(
    string Symbol, decimal Quantity, decimal AverageCost, decimal CostBasis,
    decimal? Price, decimal? MarketValue, decimal? UnrealizedGain);

public sealed record PortfolioValuation(
    Guid PortfolioId, string Currency, IReadOnlyList<ValuationLine> Lines,
    decimal TotalCostBasis, decimal? TotalMarketValue, bool IsComplete, DateTimeOffset AsOf);

/// <summary>
/// Values a portfolio using live quotes. Two reliability ideas live here:
/// 1. Quotes are cached (HybridCache: in-memory L1 + Redis L2, with stampede protection).
/// 2. Graceful degradation: if market data is down even after retries, we still answer, with IsComplete = false.
/// </summary>
public sealed partial class ValuationService(
    LedgerlyDbContext db, IMarketDataClient marketData, HybridCache cache, TimeProvider clock, ILogger<ValuationService> logger)
{
    public async Task<PortfolioValuation?> GetAsync(Guid portfolioId, string ownerId, CancellationToken ct)
    {
        var portfolio = await db.Portfolios.AsNoTracking()
            .Include(p => p.Transactions)
            .SingleOrDefaultAsync(p => p.Id == portfolioId && p.OwnerId == ownerId, ct);

        if (portfolio is null)
        {
            return null;
        }

        var holdings = HoldingCalculator.Calculate(portfolio.Transactions);
        var prices = await Task.WhenAll(holdings.Select(h => GetPriceAsync(h.Symbol, ct)));

        var lines = holdings.Zip(prices, (h, price) => new ValuationLine(
            h.Symbol, h.Quantity, h.AverageCost, h.CostBasis,
            price,
            price * h.Quantity,
            price is null ? null : (price * h.Quantity) - h.CostBasis)).ToArray();

        var complete = lines.All(l => l.Price is not null);
        return new PortfolioValuation(
            portfolio.Id,
            portfolio.BaseCurrency,
            lines,
            lines.Sum(l => l.CostBasis),
            complete ? lines.Sum(l => l.MarketValue!.Value) : null,
            complete,
            clock.GetUtcNow());
    }

    private async Task<decimal?> GetPriceAsync(string symbol, CancellationToken ct)
    {
        try
        {
            return await cache.GetOrCreateAsync(
                $"quote:{symbol}",
                async token =>
                {
                    var quotes = await marketData.GetQuotesAsync([symbol], token);
                    return quotes.TryGetValue(symbol, out var q) ? q.Price : (decimal?)null;
                },
                tags: ["quotes"],
                cancellationToken: ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or BrokenCircuitException or TimeoutRejectedException)
        {
            LogPriceUnavailable(logger, symbol, ex.GetType().Name);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Price for {Symbol} unavailable ({Reason}); returning partial valuation")]
    private static partial void LogPriceUnavailable(ILogger logger, string symbol, string reason);
}
