using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Ledgerly.Api.Services;
using Ledgerly.Core.Data;
using Ledgerly.Core.Diagnostics;
using Ledgerly.Core.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Api.Endpoints;

public sealed record CreatePortfolioRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [RegularExpression("^[A-Z]{3}$")] string? BaseCurrency);

public sealed record RenamePortfolioRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [Required] string Version);

public sealed record PortfolioSummary(Guid Id, string Name, string BaseCurrency, DateTimeOffset CreatedAt, string Version);

public sealed record PortfolioDetails(Guid Id, string Name, string BaseCurrency, string Version, IReadOnlyList<Holding> Holdings, int TransactionCount);

public sealed record RecordTransactionRequest(
    [Required, StringLength(12, MinimumLength = 1)] string Symbol,
    TransactionType Type,
    [Range(typeof(decimal), "0.000001", "1000000000")] decimal Quantity,
    [Range(typeof(decimal), "0.000001", "1000000000")] decimal Price,
    DateOnly TradeDate);

public sealed record TransactionResponse(Guid Id, string Symbol, TransactionType Type, decimal Quantity, decimal Price, DateOnly TradeDate);

public static class PortfolioEndpoints
{
    public static IEndpointRouteBuilder MapPortfolioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/portfolios")
            .WithTags("Portfolios")
            .RequireAuthorization();

        group.MapGet("/", ListPortfolios);
        group.MapPost("/", CreatePortfolio);
        group.MapGet("/{id:guid}", GetPortfolio).WithName("GetPortfolio");
        group.MapPut("/{id:guid}", RenamePortfolio);
        group.MapPost("/{id:guid}/transactions", RecordTransaction);
        group.MapGet("/{id:guid}/valuation", GetValuation);

        return app;
    }

    private static async Task<Ok<PortfolioSummary[]>> ListPortfolios(ClaimsPrincipal user, LedgerlyDbContext db, CancellationToken ct)
    {
        var ownerId = user.GetOwnerId();
        var portfolios = await db.Portfolios.AsNoTracking()
            .Where(p => p.OwnerId == ownerId)
            .OrderBy(p => p.Name)
            .ToArrayAsync(ct);

        return TypedResults.Ok(portfolios.Select(ToSummary).ToArray());
    }

    private static async Task<CreatedAtRoute<PortfolioSummary>> CreatePortfolio(
        CreatePortfolioRequest request, ClaimsPrincipal user, LedgerlyDbContext db, CancellationToken ct)
    {
        var portfolio = new Portfolio
        {
            OwnerId = user.GetOwnerId(),
            Name = request.Name.Trim(),
            BaseCurrency = request.BaseCurrency ?? "EUR",
        };

        db.Portfolios.Add(portfolio);
        await db.SaveChangesAsync(ct);

        return TypedResults.CreatedAtRoute(ToSummary(portfolio), "GetPortfolio", new { id = portfolio.Id });
    }

    private static async Task<Results<Ok<PortfolioDetails>, NotFound>> GetPortfolio(
        Guid id, ClaimsPrincipal user, LedgerlyDbContext db, CancellationToken ct)
    {
        var ownerId = user.GetOwnerId();
        var portfolio = await db.Portfolios.AsNoTracking()
            .Include(p => p.Transactions)
            .SingleOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId, ct);

        if (portfolio is null)
        {
            return TypedResults.NotFound();
        }

        var holdings = HoldingCalculator.Calculate(portfolio.Transactions);
        return TypedResults.Ok(new PortfolioDetails(
            portfolio.Id, portfolio.Name, portfolio.BaseCurrency, Convert.ToBase64String(portfolio.RowVersion), holdings, portfolio.Transactions.Count));
    }

    /// <summary>Optimistic concurrency: the client sends back the Version it read; a stale version gets 409.</summary>
    private static async Task<Results<Ok<PortfolioSummary>, NotFound, ProblemHttpResult>> RenamePortfolio(
        Guid id, RenamePortfolioRequest request, ClaimsPrincipal user, LedgerlyDbContext db, CancellationToken ct)
    {
        var ownerId = user.GetOwnerId();
        var portfolio = await db.Portfolios.SingleOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId, ct);
        if (portfolio is null)
        {
            return TypedResults.NotFound();
        }

        db.Entry(portfolio).Property(p => p.RowVersion).OriginalValue = Convert.FromBase64String(request.Version);
        portfolio.Name = request.Name.Trim();
        portfolio.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Problem("Portfolio was changed by someone else. Reload and try again.", statusCode: StatusCodes.Status409Conflict);
        }

        return TypedResults.Ok(ToSummary(portfolio));
    }

    private static async Task<Results<Created<TransactionResponse>, NotFound, ValidationProblem, ProblemHttpResult>> RecordTransaction(
        Guid id, RecordTransactionRequest request, ClaimsPrincipal user, LedgerlyDbContext db, CancellationToken ct)
    {
        var ownerId = user.GetOwnerId();
        var portfolio = await db.Portfolios
            .Include(p => p.Transactions)
            .SingleOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId, ct);

        if (portfolio is null)
        {
            return TypedResults.NotFound();
        }

        var transaction = new Transaction
        {
            PortfolioId = portfolio.Id,
            Symbol = request.Symbol.Trim().ToUpperInvariant(),
            Type = request.Type,
            Quantity = request.Quantity,
            Price = request.Price,
            TradeDate = request.TradeDate,
        };

        try
        {
            // Domain rule: you cannot sell what you do not hold (at that date).
            HoldingCalculator.Calculate([.. portfolio.Transactions, transaction]);
        }
        catch (HoldingsException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = [ex.Message] });
        }

        // EF trap: the Id is already set (Guid v7, generated in C#). Adding via portfolio.Transactions would make EF
        // assume the row EXISTS and send an UPDATE (0 rows -> DbUpdateConcurrencyException). Add it explicitly.
        db.Transactions.Add(transaction);
        portfolio.UpdatedAt = DateTimeOffset.UtcNow; // bumps rowversion: two parallel "sell all" requests cannot both win

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Problem("Portfolio changed while recording the trade. Retry the request.", statusCode: StatusCodes.Status409Conflict);
        }

        LedgerlyTelemetry.TransactionsRecorded.Add(1, new KeyValuePair<string, object?>("source", "api"));

        return TypedResults.Created(
            $"/api/portfolios/{id}/transactions/{transaction.Id}",
            new TransactionResponse(transaction.Id, transaction.Symbol, transaction.Type, transaction.Quantity, transaction.Price, transaction.TradeDate));
    }

    private static async Task<Results<Ok<PortfolioValuation>, NotFound>> GetValuation(
        Guid id, ClaimsPrincipal user, ValuationService valuations, CancellationToken ct)
    {
        var valuation = await valuations.GetAsync(id, user.GetOwnerId(), ct);
        return valuation is null ? TypedResults.NotFound() : TypedResults.Ok(valuation);
    }

    private static PortfolioSummary ToSummary(Portfolio p) =>
        new(p.Id, p.Name, p.BaseCurrency, p.CreatedAt, Convert.ToBase64String(p.RowVersion));
}
