using Microsoft.AspNetCore.Http;

namespace Api.Features.Notifications;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        g.MapPost("booking-reminder/{bookingId:guid}", async Task<IResult> (
            Guid bookingId, INotificationService svc, CancellationToken ct) =>
        {
            var result = await svc.SendBookingReminderAsync(bookingId, ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithOpenApi(op => { op.Summary = "Send SMS appointment reminder for a booking"; return op; });

        g.MapPost("prescription-ready", async Task<IResult> (
            PrescriptionReadyRequest req, INotificationService svc, CancellationToken ct) =>
        {
            var result = await svc.SendPrescriptionReadyAsync(req.ClientId, req.Message, ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithOpenApi(op => { op.Summary = "Send prescription ready SMS to a client"; return op; });

        return app;
    }
}
