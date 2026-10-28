using System.Net.Http.Json;

namespace Ledgerly.Core.Market;

public sealed record Quote(string Symbol, decimal Price, string Currency, DateTimeOffset AsOf);

public interface IMarketDataClient
{
    Task<IReadOnlyDictionary<string, Quote>> GetQuotesAsync(IReadOnlyCollection<string> symbols, CancellationToken cancellationToken);
}

/// <summary>
/// Typed HttpClient for the market-data service. Base address, retries, timeouts and the circuit breaker
/// are configured at registration time (see ServiceDefaults), not here. This class just speaks HTTP.
/// </summary>
public sealed class MarketDataClient(HttpClient http) : IMarketDataClient
{
    public async Task<IReadOnlyDictionary<string, Quote>> GetQuotesAsync(IReadOnlyCollection<string> symbols, CancellationToken cancellationToken)
    {
        if (symbols.Count == 0)
        {
            return new Dictionary<string, Quote>();
        }

        var query = Uri.EscapeDataString(string.Join(',', symbols));
        var quotes = await http.GetFromJsonAsync<Quote[]>($"/prices?symbols={query}", cancellationToken) ?? [];
        return quotes.ToDictionary(q => q.Symbol, StringComparer.OrdinalIgnoreCase);
    }
}
