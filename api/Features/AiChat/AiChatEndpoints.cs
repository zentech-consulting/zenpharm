using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Api.Features.AiChat;

public static class AiChatEndpoints
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IEndpointRouteBuilder MapAiChatEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/ai-chat")
            .WithTags("AI Chat")
            .AllowAnonymous()
            .RequireRateLimiting("ai-chat");

        g.MapPost("", async Task<IResult> (AiChatRequest req, IAiChatManager mgr, HttpContext ctx, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Message) || req.Message.Length > 4000)
                return Results.BadRequest("Message is required and must be under 4,000 characters.");

            var clientIp = ctx.Connection.RemoteIpAddress?.ToString();
            var result = await mgr.SendAsync(req, clientIp, ct);
            return Results.Ok(result);
        })
        .WithOpenApi(op => { op.Summary = "Send a message to the AI consultant"; return op; });

        g.MapPost("stream", async (AiChatRequest req, IAiChatManager mgr, HttpContext ctx, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Message) || req.Message.Length > 4000)
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.WriteAsync("Message is required and must be under 4,000 characters.", ct);
                return;
            }

            var clientIp = ctx.Connection.RemoteIpAddress?.ToString();

            ctx.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";
            ctx.Response.Headers["X-Accel-Buffering"] = "no";
            ctx.Response.Headers.ContentEncoding = "identity";

            await foreach (var evt in mgr.SendStreamAsync(req, clientIp, ct))
            {
                var json = JsonSerializer.Serialize(evt, s_jsonOptions);
                await ctx.Response.WriteAsync($"event: {evt.Type}\ndata: {json}\n\n", ct);
                await ctx.Response.Body.FlushAsync(ct);
            }
        })
        .WithOpenApi(op => { op.Summary = "Stream a response from the AI consultant (SSE)"; return op; });

        return app;
    }
}
