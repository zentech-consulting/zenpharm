using Microsoft.AspNetCore.Http;

namespace Api.Features.Schedules;

public static class ScheduleEndpoints
{
    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/schedules")
            .WithTags("Schedules")
            .RequireAuthorization();

        g.MapPost("", async Task<IResult> (CreateScheduleRequest req, IScheduleManager mgr, CancellationToken ct) =>
        {
            var schedule = await mgr.CreateAsync(req, ct);
            return Results.Created($"/api/schedules/{schedule.Id}", schedule);
        })
        .WithOpenApi(op => { op.Summary = "Create a schedule entry"; return op; });

        g.MapGet("{id:guid}", async Task<IResult> (Guid id, IScheduleManager mgr, CancellationToken ct) =>
        {
            var schedule = await mgr.GetByIdAsync(id, ct);
            return schedule is not null ? Results.Ok(schedule) : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Get a schedule entry by ID"; return op; });

        g.MapGet("", async Task<IResult> (DateOnly? startDate, DateOnly? endDate, Guid? employeeId, IScheduleManager mgr, CancellationToken ct) =>
        {
            var result = await mgr.ListAsync(startDate, endDate, employeeId, ct);
            return Results.Ok(result);
        })
        .WithOpenApi(op => { op.Summary = "List schedule entries with filters"; return op; });

        g.MapPut("{id:guid}", async Task<IResult> (Guid id, UpdateScheduleRequest req, IScheduleManager mgr, CancellationToken ct) =>
        {
            var schedule = await mgr.UpdateAsync(id, req, ct);
            return schedule is not null ? Results.Ok(schedule) : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Update a schedule entry"; return op; });

        g.MapDelete("{id:guid}", async Task<IResult> (Guid id, IScheduleManager mgr, CancellationToken ct) =>
        {
            var deleted = await mgr.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Delete a schedule entry"; return op; });

        g.MapPost("generate", async Task<IResult> (GenerateScheduleRequest req, IScheduleManager mgr, CancellationToken ct) =>
        {
            var schedules = await mgr.GenerateAsync(req, ct);
            return Results.Ok(schedules);
        })
        .WithOpenApi(op => { op.Summary = "Auto-generate schedules for a date range"; return op; });

        return app;
    }
}
