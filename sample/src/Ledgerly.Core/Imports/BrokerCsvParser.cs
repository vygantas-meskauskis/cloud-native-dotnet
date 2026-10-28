using System.Globalization;
using Ledgerly.Core.Domain;

namespace Ledgerly.Core.Imports;

public sealed record ParsedTrade(int Line, DateOnly TradeDate, TransactionType Type, string Symbol, decimal Quantity, decimal Price, string Reference);

public sealed record ParseError(int Line, string Message);

public sealed record ParseResult(IReadOnlyList<ParsedTrade> Trades, IReadOnlyList<ParseError> Errors);

/// <summary>
/// Parses the broker export format:
/// <c>date,type,symbol,quantity,price,reference</c> (header required, ISO dates, invariant decimals).
/// Bad rows are reported, not thrown: one typo should not reject a 5 000-row file.
/// </summary>
public static class BrokerCsvParser
{
    private const string ExpectedHeader = "date,type,symbol,quantity,price,reference";

    public static ParseResult Parse(string content)
    {
        var trades = new List<ParsedTrade>();
        var errors = new List<ParseError>();
        var lines = content.ReplaceLineEndings("\n").Split('\n');

        if (lines.Length == 0 || !string.Equals(lines[0].Trim(), ExpectedHeader, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new ParseError(1, $"Expected header '{ExpectedHeader}'."));
            return new ParseResult(trades, errors);
        }

        for (var i = 1; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var cells = line.Split(',');
            if (cells.Length != 6)
            {
                errors.Add(new ParseError(lineNumber, $"Expected 6 columns but found {cells.Length}."));
                continue;
            }

            if (!DateOnly.TryParseExact(cells[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                errors.Add(new ParseError(lineNumber, $"Invalid date '{cells[0]}'."));
                continue;
            }

            if (!Enum.TryParse<TransactionType>(cells[1], ignoreCase: true, out var type))
            {
                errors.Add(new ParseError(lineNumber, $"Invalid type '{cells[1]}' (use buy or sell)."));
                continue;
            }

            var symbol = cells[2].Trim().ToUpperInvariant();
            if (symbol.Length is 0 or > 12)
            {
                errors.Add(new ParseError(lineNumber, "Symbol must be 1-12 characters."));
                continue;
            }

            if (!decimal.TryParse(cells[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
            {
                errors.Add(new ParseError(lineNumber, $"Invalid quantity '{cells[3]}'."));
                continue;
            }

            if (!decimal.TryParse(cells[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0)
            {
                errors.Add(new ParseError(lineNumber, $"Invalid price '{cells[4]}'."));
                continue;
            }

            var reference = cells[5].Trim();
            if (reference.Length is 0 or > 64)
            {
                errors.Add(new ParseError(lineNumber, "Reference must be 1-64 characters."));
                continue;
            }

            trades.Add(new ParsedTrade(lineNumber, date, type, symbol, quantity, price, reference));
        }

        return new ParseResult(trades, errors);
    }
}
