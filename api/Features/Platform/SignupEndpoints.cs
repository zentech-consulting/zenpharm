namespace Api.Features.Platform;

public static class SignupEndpoints
{
    public static void MapSignupEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/signup")
            .WithTags("Signup")
            .AllowAnonymous();

        group.MapPost("/checkout", async (
            CreateCheckoutRequest req,
            ISignupManager signupManager,
            CancellationToken ct) =>
        {
            try
            {
                var result = await signupManager.CreateCheckoutAsync(req, ct);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireRateLimiting("signup-checkout")
        .WithOpenApi(op => { op.Summary = "Create a Stripe Checkout session for signup"; return op; });

        group.MapGet("/status/{sessionId}", async (
            string sessionId,
            ISignupManager signupManager,
            CancellationToken ct) =>
        {
            var result = await signupManager.GetStatusAsync(sessionId, ct);
            return Results.Ok(result);
        })
        .WithOpenApi(op => { op.Summary = "Check signup provisioning status"; return op; });

        group.MapGet("/plans", async (
            ISignupManager signupManager,
            CancellationToken ct) =>
        {
            var plans = await signupManager.ListPlansAsync(ct);
            return Results.Ok(plans);
        })
        .WithOpenApi(op => { op.Summary = "List available subscription plans"; return op; });

        group.MapGet("/check-subdomain/{subdomain}", async (
            string subdomain,
            ISignupManager signupManager,
            CancellationToken ct) =>
        {
            var available = await signupManager.CheckSubdomainAsync(subdomain, ct);
            return Results.Ok(new { subdomain, available });
        })
        .WithOpenApi(op => { op.Summary = "Check if a subdomain is available"; return op; });
    }
}
