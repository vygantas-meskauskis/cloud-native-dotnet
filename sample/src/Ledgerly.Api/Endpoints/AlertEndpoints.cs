using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Ledgerly.Core.Data;
using Ledgerly.Core.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace Ledgerly.Api.Endpoints;

public sealed record CreateAlertRequest(
    [Required, StringLength(12, MinimumLength = 1)] string Symbol,
    [Range(typeof(decimal), "0.000001", "1000000000")] decimal TargetPrice,
    AlertDirection Direction);

public sealed record AlertResponse(Guid Id, string Symbol, decimal TargetPrice, AlertDirection Direction, bool IsActive, DateTimeOffset? TriggeredAt);

public sealed record NotificationResponse(Guid Id, string Message, DateTimeOffset CreatedAt);

public static class AlertEndpoints
{
    public static IEndpointRouteBuilder MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var alerts = app.MapGroup("/api/alerts")
            .WithTags("Alerts")
            .RequireAuthorization()
            .AddEndpointFilter(RequireFeature(Features.PriceAlerts));

        alerts.MapGet("/", ListAlerts);
        alerts.MapPost("/", CreateAlert);
        alerts.MapDelete("/{id:guid}", DeleteAlert);

        app.MapGet("/api/notifications", ListNotifications)
            .WithTags("Alerts")
            .RequireAuthorization();

        return app;
    }

    /// <summary>Feature flag gate: when the flag is off the endpoints behave as if they do not exist.</summary>
    private static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> RequireFeature(string feature) =>
        async (context, next) =>
        {
            var features = context.HttpContext.RequestServices.GetRequiredService<IFeatureManager>();
            return await features.IsEnabledAsync(feature) ? await next(context) : TypedResults.NotFound();
        };

    private static async Task<Ok<AlertResponse[]>> ListAlerts(ClaimsPrincipal user, LedgerlyDbContext db, CancellationToken ct)
    {
        var ownerId = user.GetOwnerId();
        var alerts = await db.PriceAlerts.AsNoTracking().Where(a => a.OwnerId == ownerId).ToArrayAsync(ct);
        return TypedResults.Ok(alerts.Select(ToResponse).ToArray());
    }

    private static async Task<Created<AlertResponse>> CreateAlert(CreateAlertRequest request, ClaimsPrincipal user, LedgerlyDbContext db, CancellationToken ct)
    {
        var alert = new PriceAlert
        {
            OwnerId = user.GetOwnerId(),
            Symbol = request.Symbol.Trim().ToUpperInvariant(),
            TargetPrice = request.TargetPrice,
            Direction = request.Direction,
        };

        db.PriceAlerts.Add(alert);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/api/alerts/{alert.Id}", ToResponse(alert));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAlert(Guid id, ClaimsPrincipal user, LedgerlyDbContext db, CancellationToken ct)
    {
        var ownerId = user.GetOwnerId();
        var deleted = await db.PriceAlerts.Where(a => a.Id == id && a.OwnerId == ownerId).ExecuteDeleteAsync(ct);
        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    private static async Task<Ok<NotificationResponse[]>> ListNotifications(ClaimsPrincipal user, LedgerlyDbContext db, CancellationToken ct)
    {
        var ownerId = user.GetOwnerId();
        var items = await db.Notifications.AsNoTracking()
            .Where(n => n.OwnerId == ownerId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationResponse(n.Id, n.Message, n.CreatedAt))
            .ToArrayAsync(ct);
        return TypedResults.Ok(items);
    }

    private static AlertResponse ToResponse(PriceAlert a) => new(a.Id, a.Symbol, a.TargetPrice, a.Direction, a.IsActive, a.TriggeredAt);
}
