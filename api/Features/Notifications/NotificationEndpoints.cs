using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Api.Features.Notifications;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization()
            .RequireRateLimiting("notifications");

        g.MapPost("booking-reminder/{bookingId:guid}", async Task<IResult> (
            Guid bookingId, INotificationService svc, HttpContext ctx, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirstValue(ClaimTypes.Role);
            if (role is not ("Admin" or "SuperAdmin" or "Manager" or "Pharmacist"))
                return Results.Forbid();

            var result = await svc.SendBookingReminderAsync(bookingId, ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithOpenApi(op => { op.Summary = "Send SMS appointment reminder for a booking"; return op; });

        g.MapPost("prescription-ready", async Task<IResult> (
            PrescriptionReadyRequest req, INotificationService svc, HttpContext ctx, CancellationToken ct) =>
        {
            var role = ctx.User.FindFirstValue(ClaimTypes.Role);
            if (role is not ("Admin" or "SuperAdmin" or "Manager" or "Pharmacist"))
                return Results.Forbid();

            var result = await svc.SendPrescriptionReadyAsync(req.ClientId, req.Message, ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithOpenApi(op => { op.Summary = "Send prescription ready SMS to a client"; return op; });

        return app;
    }
}
