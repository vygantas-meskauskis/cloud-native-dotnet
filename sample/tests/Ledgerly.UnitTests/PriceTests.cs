using Ledgerly.Core.Domain;
using Ledgerly.MarketData;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Ledgerly.UnitTests;

public class PriceAlertAndPriceGeneratorTests
{
    [Theory]
    [InlineData(AlertDirection.Above, 100, 100, true)]
    [InlineData(AlertDirection.Above, 100, 99.99, false)]
    [InlineData(AlertDirection.Below, 100, 100, true)]
    [InlineData(AlertDirection.Below, 100, 100.01, false)]
    public void Alert_trigger_rules(AlertDirection direction, decimal target, decimal price, bool expected)
    {
        var alert = new PriceAlert { OwnerId = "u1", Symbol = "AAPL", TargetPrice = target, Direction = direction };
        Assert.Equal(expected, alert.IsTriggeredBy(price));
    }

    [Fact]
    public void Prices_are_stable_within_a_minute_and_move_over_time()
    {
        // TimeProvider makes "time" a dependency you can control in tests. No more DateTime.Now in domain code.
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero));
        var generator = new PriceGenerator(clock);

        var first = generator.GetQuote("aapl");
        clock.Advance(TimeSpan.FromSeconds(20));
        var sameMinute = generator.GetQuote("AAPL");
        clock.Advance(TimeSpan.FromHours(3));
        var later = generator.GetQuote("AAPL");

        Assert.Equal("AAPL", first.Symbol);
        Assert.Equal(first.Price, sameMinute.Price);
        Assert.NotEqual(first.Price, later.Price);
        Assert.True(first.Price > 0);
    }
}
