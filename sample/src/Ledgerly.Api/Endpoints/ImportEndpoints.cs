using System.Security.Claims;
using Ledgerly.Core.Data;
using Ledgerly.Core.Domain;
using Ledgerly.Core.Messaging;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Api.Endpoints;

public sealed record ImportStatusResponse(
    Guid Id, Guid PortfolioId, string FileName, ImportStatus Status, int RowsImported, int RowsSkipped, string? Error,
    DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);

public static class ImportEndpoints
{
    private const long MaxFileBytes = 1_000_000;

    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Imports").RequireAuthorization();

        group.MapPost("/portfolios/{id:guid}/imports", StartImport)
            .RequireRateLimiting(ApiPolicies.Imports)
            // Bearer-token APIs are not vulnerable to CSRF (browsers never attach the token automatically).
            .DisableAntiforgery();

        group.MapGet("/imports/{importId:guid}", GetImport).WithName("GetImport");
        return app;
    }

    /// <summary>
    /// Accepts a broker CSV and returns 202 immediately. The import row and the outbox message are saved in
    /// ONE transaction; the worker relays the message to Service Bus and does the heavy lifting.
    /// </summary>
    private static async Task<Results<AcceptedAtRoute<ImportStatusResponse>, NotFound, ValidationProblem>> StartImport(
        Guid id, IFormFile file, ClaimsPrincipal user, LedgerlyDbContext db, CancellationToken ct)
    {
        if (file.Length is 0 or > MaxFileBytes)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = [$"File must be between 1 byte and {MaxFileBytes} bytes."] });
        }

        var ownerId = user.GetOwnerId();
        if (!await db.Portfolios.AnyAsync(p => p.Id == id && p.OwnerId == ownerId, ct))
        {
            return TypedResults.NotFound();
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var import = new Import
        {
            PortfolioId = id,
            OwnerId = ownerId,
            FileName = Path.GetFileName(file.FileName),
            Content = await reader.ReadToEndAsync(ct),
        };

        db.Imports.Add(import);
        db.EnqueueOutboxMessage(Queues.Imports, new ImportRequested(import.Id, id));
        await db.SaveChangesAsync(ct); // one transaction: both rows or neither

        return TypedResults.AcceptedAtRoute(ToResponse(import), "GetImport", new { importId = import.Id });
    }

    private static async Task<Results<Ok<ImportStatusResponse>, NotFound>> GetImport(
        Guid importId, ClaimsPrincipal user, LedgerlyDbContext db, CancellationToken ct)
    {
        var ownerId = user.GetOwnerId();
        var import = await db.Imports.AsNoTracking().SingleOrDefaultAsync(i => i.Id == importId && i.OwnerId == ownerId, ct);
        return import is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(import));
    }

    private static ImportStatusResponse ToResponse(Import i) =>
        new(i.Id, i.PortfolioId, i.FileName, i.Status, i.RowsImported, i.RowsSkipped, i.Error, i.CreatedAt, i.CompletedAt);
}
