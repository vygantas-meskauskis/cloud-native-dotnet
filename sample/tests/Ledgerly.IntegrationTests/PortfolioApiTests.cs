using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ledgerly.Api.Endpoints;
using Ledgerly.Api.Services;
using Ledgerly.Core.Domain;
using Xunit;

namespace Ledgerly.IntegrationTests;

public sealed class PortfolioApiTests(LedgerlyApiFactory factory) : IClassFixture<LedgerlyApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private HttpClient ClientFor(string user)
    {
        Assert.SkipWhen(factory.SkipReason is not null, factory.SkipReason ?? string.Empty);
        return factory.CreateClientFor(user);
    }

    [Fact]
    public async Task Anonymous_requests_are_rejected()
    {
        Assert.SkipWhen(factory.SkipReason is not null, factory.SkipReason ?? string.Empty);
        var response = await factory.CreateClient().GetAsync(new Uri("/api/portfolios", UriKind.Relative), Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_then_read_back()
    {
        var client = ClientFor("alice");

        var created = await client.PostAsJsonAsync("/api/portfolios", new { name = "Long term", baseCurrency = "EUR" }, Ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);

        var details = await client.GetFromJsonAsync<PortfolioDetails>(created.Headers.Location, Json, Ct);
        Assert.Equal("Long term", details!.Name);
        Assert.Empty(details.Holdings);
    }

    [Fact]
    public async Task Invalid_input_returns_problem_details()
    {
        var client = ClientFor("alice");

        var response = await client.PostAsJsonAsync("/api/portfolios", new { name = "", baseCurrency = "euro" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Users_cannot_see_each_others_portfolios()
    {
        var alice = ClientFor("alice");
        var bob = ClientFor("bob");

        var created = await alice.PostAsJsonAsync("/api/portfolios", new { name = "Private" }, Ct);
        var response = await bob.GetAsync(created.Headers.Location, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Overselling_is_a_validation_error()
    {
        var client = ClientFor("carol");
        var portfolio = await CreatePortfolioAsync(client);

        await BuyAsync(client, portfolio.Id, "AAPL", 5);
        var oversell = await PostTradeAsync(client, portfolio.Id, "AAPL", TransactionType.Sell, 6);

        Assert.Equal(HttpStatusCode.BadRequest, oversell.StatusCode);
    }

    [Fact]
    public async Task Stale_version_gets_409_conflict()
    {
        var client = ClientFor("dave");
        var portfolio = await CreatePortfolioAsync(client);

        var first = await client.PutAsJsonAsync($"/api/portfolios/{portfolio.Id}", new { name = "Renamed once", version = portfolio.Version }, Ct);
        var second = await client.PutAsJsonAsync($"/api/portfolios/{portfolio.Id}", new { name = "Renamed twice", version = portfolio.Version }, Ct);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Valuation_degrades_gracefully_when_market_data_is_down()
    {
        var client = ClientFor("erin");
        var portfolio = await CreatePortfolioAsync(client);
        await BuyAsync(client, portfolio.Id, "ZZDOWN", 2);

        factory.MarketData.IsDown = true;
        try
        {
            var valuation = await client.GetFromJsonAsync<PortfolioValuation>($"/api/portfolios/{portfolio.Id}/valuation", Json, Ct);

            Assert.False(valuation!.IsComplete);
            Assert.Null(valuation.TotalMarketValue);
            Assert.Equal(200m, valuation.TotalCostBasis);
        }
        finally
        {
            factory.MarketData.IsDown = false;
        }
    }

    [Fact]
    public async Task Valuation_uses_market_prices()
    {
        var client = ClientFor("frank");
        var portfolio = await CreatePortfolioAsync(client);
        await BuyAsync(client, portfolio.Id, "ZZUP", 2);
        factory.MarketData.Prices["ZZUP"] = 150m;

        var valuation = await client.GetFromJsonAsync<PortfolioValuation>($"/api/portfolios/{portfolio.Id}/valuation", Json, Ct);

        Assert.True(valuation!.IsComplete);
        Assert.Equal(300m, valuation.TotalMarketValue);
        Assert.Equal(100m, Assert.Single(valuation.Lines).UnrealizedGain);
    }

    private static async Task<PortfolioSummary> CreatePortfolioAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/portfolios", new { name = $"P-{Guid.NewGuid():N}" }, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PortfolioSummary>(Json, Ct))!;
    }

    private static async Task BuyAsync(HttpClient client, Guid portfolioId, string symbol, decimal quantity)
    {
        var response = await PostTradeAsync(client, portfolioId, symbol, TransactionType.Buy, quantity);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static Task<HttpResponseMessage> PostTradeAsync(HttpClient client, Guid portfolioId, string symbol, TransactionType type, decimal quantity) =>
        client.PostAsJsonAsync(
            $"/api/portfolios/{portfolioId}/transactions",
            new RecordTransactionRequest(symbol, type, quantity, 100m, new DateOnly(2026, 9, 1)),
            Json,
            Ct);
}
