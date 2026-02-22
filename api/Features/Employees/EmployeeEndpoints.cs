using Microsoft.AspNetCore.Http;

namespace Api.Features.Employees;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/employees")
            .WithTags("Employees")
            .RequireAuthorization();

        g.MapPost("", async Task<IResult> (CreateEmployeeRequest req, IEmployeeManager mgr, CancellationToken ct) =>
        {
            var employee = await mgr.CreateAsync(req, ct);
            return Results.Created($"/api/employees/{employee.Id}", employee);
        })
        .WithOpenApi(op => { op.Summary = "Create a new employee"; return op; });

        g.MapGet("{id:guid}", async Task<IResult> (Guid id, IEmployeeManager mgr, CancellationToken ct) =>
        {
            var employee = await mgr.GetByIdAsync(id, ct);
            return employee is not null ? Results.Ok(employee) : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Get an employee by ID"; return op; });

        g.MapGet("", async Task<IResult> (int? page, int? pageSize, string? role, IEmployeeManager mgr, CancellationToken ct) =>
        {
            var result = await mgr.ListAsync(page ?? 1, pageSize ?? 20, role, ct);
            return Results.Ok(result);
        })
        .WithOpenApi(op => { op.Summary = "List employees with pagination"; return op; });

        g.MapPut("{id:guid}", async Task<IResult> (Guid id, UpdateEmployeeRequest req, IEmployeeManager mgr, CancellationToken ct) =>
        {
            var employee = await mgr.UpdateAsync(id, req, ct);
            return employee is not null ? Results.Ok(employee) : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Update an employee"; return op; });

        g.MapDelete("{id:guid}", async Task<IResult> (Guid id, IEmployeeManager mgr, CancellationToken ct) =>
        {
            var deleted = await mgr.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithOpenApi(op => { op.Summary = "Delete an employee"; return op; });

        return app;
    }
}
