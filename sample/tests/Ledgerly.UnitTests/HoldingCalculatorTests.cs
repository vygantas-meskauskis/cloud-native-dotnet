using Ledgerly.Core.Domain;
using Xunit;

namespace Ledgerly.UnitTests;

public class HoldingCalculatorTests
{
    private static readonly Guid Portfolio = Guid.NewGuid();

    private static Transaction Trade(string symbol, TransactionType type, decimal qty, decimal price, int day) => new()
    {
        PortfolioId = Portfolio,
        Symbol = symbol,
        Type = type,
        Quantity = qty,
        Price = price,
        TradeDate = new DateOnly(2026, 1, day),
    };

    [Fact]
    public void Buys_are_averaged()
    {
        var holdings = HoldingCalculator.Calculate(
        [
            Trade("VWCE", TransactionType.Buy, 10, 100m, 1),
            Trade("VWCE", TransactionType.Buy, 10, 120m, 2),
        ]);

        var vwce = Assert.Single(holdings);
        Assert.Equal(20, vwce.Quantity);
        Assert.Equal(110m, vwce.AverageCost);
    }

    [Fact]
    public void Selling_keeps_the_average_cost_of_the_rest()
    {
        var holdings = HoldingCalculator.Calculate(
        [
            Trade("AAPL", TransactionType.Buy, 10, 100m, 1),
            Trade("AAPL", TransactionType.Sell, 4, 150m, 2),
        ]);

        var aapl = Assert.Single(holdings);
        Assert.Equal(6, aapl.Quantity);
        Assert.Equal(100m, aapl.AverageCost);
    }

    [Fact]
    public void Fully_sold_positions_disappear()
    {
        var holdings = HoldingCalculator.Calculate(
        [
            Trade("MSFT", TransactionType.Buy, 3, 400m, 1),
            Trade("MSFT", TransactionType.Sell, 3, 420m, 2),
        ]);

        Assert.Empty(holdings);
    }

    [Fact]
    public void Cannot_sell_more_than_held_at_that_date()
    {
        var ex = Assert.Throws<HoldingsException>(() => HoldingCalculator.Calculate(
        [
            Trade("NVDA", TransactionType.Sell, 1, 100m, 1),
            Trade("NVDA", TransactionType.Buy, 5, 90m, 2),
        ]));

        Assert.Contains("NVDA", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TransactionType.Buy, 5)]
    [InlineData(TransactionType.Sell, 1)]
    public void Symbols_are_case_insensitive(TransactionType secondType, decimal expectedRemainder)
    {
        var holdings = HoldingCalculator.Calculate(
        [
            Trade("sap", TransactionType.Buy, 3, 100m, 1),
            Trade("SAP", secondType, 2, 100m, 2),
        ]);

        var sap = Assert.Single(holdings);
        Assert.Equal("SAP", sap.Symbol);
        Assert.Equal(expectedRemainder, sap.Quantity);
    }
}
