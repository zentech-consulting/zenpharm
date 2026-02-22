using Microsoft.AspNetCore.Http;

namespace Api.Features.Bookings;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/bookings")
            .WithTags("Bookings");

        g.MapPost("", async Task<IResult> (CreateBookingRequest req, IBookingManager mgr, CancellationToken ct) =>
        {
            var booking = await mgr.CreateAsync(req, ct);
            return Results.Created($"/api/bookings/{booking.Id}", booking);
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Create a new booking"; return op; });

        g.MapGet("{id:guid}", async Task<IResult> (Guid id, IBookingManager mgr, CancellationToken ct) =>
        {
            var booking = await mgr.GetByIdAsync(id, ct);
            return booking is not null ? Results.Ok(booking) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Get a booking by ID"; return op; });

        g.MapGet("", async Task<IResult> (int? page, int? pageSize, DateOnly? date, Guid? employeeId, IBookingManager mgr, CancellationToken ct) =>
        {
            var result = await mgr.ListAsync(page ?? 1, pageSize ?? 20, date, employeeId, ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "List bookings with filters"; return op; });

        g.MapPut("{id:guid}", async Task<IResult> (Guid id, UpdateBookingRequest req, IBookingManager mgr, CancellationToken ct) =>
        {
            var booking = await mgr.UpdateAsync(id, req, ct);
            return booking is not null ? Results.Ok(booking) : Results.NotFound();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Update a booking"; return op; });

        g.MapDelete("{id:guid}", async Task<IResult> (Guid id, IBookingManager mgr, CancellationToken ct) =>
        {
            var cancelled = await mgr.CancelAsync(id, ct);
            return cancelled ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization()
        .WithOpenApi(op => { op.Summary = "Cancel a booking"; return op; });

        g.MapGet("available-slots", async Task<IResult> (Guid serviceId, DateOnly date, Guid? employeeId, IBookingManager mgr, CancellationToken ct) =>
        {
            var slots = await mgr.GetAvailableSlotsAsync(serviceId, date, employeeId, ct);
            return Results.Ok(slots);
        })
        .AllowAnonymous()
        .WithOpenApi(op => { op.Summary = "Get available booking slots for a service"; return op; });

        return app;
    }
}
