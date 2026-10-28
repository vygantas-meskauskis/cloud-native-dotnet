using Ledgerly.Core.Domain;
using Ledgerly.Core.Imports;
using Xunit;

namespace Ledgerly.UnitTests;

public class BrokerCsvParserTests
{
    private const string Header = "date,type,symbol,quantity,price,reference";

    [Fact]
    public void Parses_valid_rows()
    {
        var result = BrokerCsvParser.Parse($"""
            {Header}
            2026-09-01,buy,aapl,5,190.10,BRK-1
            2026-09-02,SELL,AAPL,2,195.5,BRK-2
            """);

        Assert.Empty(result.Errors);
        Assert.Collection(result.Trades,
            t =>
            {
                Assert.Equal("AAPL", t.Symbol);
                Assert.Equal(TransactionType.Buy, t.Type);
                Assert.Equal(190.10m, t.Price);
            },
            t => Assert.Equal(TransactionType.Sell, t.Type));
    }

    [Fact]
    public void Bad_rows_are_reported_not_thrown()
    {
        var result = BrokerCsvParser.Parse($"""
            {Header}
            2026-09-01,buy,AAPL,5,190.10,BRK-1
            01/09/2026,buy,AAPL,5,190.10,BRK-2
            2026-09-03,hold,AAPL,5,190.10,BRK-3
            2026-09-04,buy,AAPL,-1,190.10,BRK-4
            2026-09-05,buy,AAPL,5,190,10,BRK-5
            """);

        Assert.Single(result.Trades);
        Assert.Equal([3, 4, 5, 6], result.Errors.Select(e => e.Line));
    }

    [Fact]
    public void Decimal_parsing_ignores_server_culture()
    {
        // The old IIS box had lt-LT culture and "190,10" broke imports. Invariant culture, always.
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("lt-LT");
        try
        {
            var result = BrokerCsvParser.Parse($"{Header}\n2026-09-01,buy,AAPL,5,190.10,BRK-1");
            Assert.Equal(190.10m, Assert.Single(result.Trades).Price);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Missing_header_is_an_error()
    {
        var result = BrokerCsvParser.Parse("2026-09-01,buy,AAPL,5,190.10,BRK-1");

        Assert.Empty(result.Trades);
        Assert.Equal(1, Assert.Single(result.Errors).Line);
    }
}
