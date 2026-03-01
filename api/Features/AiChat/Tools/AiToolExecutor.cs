using System.Text.Json;
using Api.Features.Bookings;
using Api.Features.Knowledge;
using Api.Features.Services;

namespace Api.Features.AiChat.Tools;

internal sealed class AiToolExecutor(
    IServiceProvider serviceProvider,
    ILogger<AiToolExecutor> logger) : IAiToolExecutor
{
    public async Task<ToolExecutionResult> ExecuteAsync(string toolName, JsonElement input, CancellationToken ct = default)
    {
        logger.LogInformation("Executing tool: {ToolName}", toolName);

        try
        {
            return toolName switch
            {
                "search_knowledge" => await ExecuteSearchKnowledgeAsync(input, ct),
                "list_services" => await ExecuteListServicesAsync(input, ct),
                "check_availability" => await ExecuteCheckAvailabilityAsync(input, ct),
                _ => ToolExecutionResult.Fail($"Unknown tool: {toolName}")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tool execution failed: {ToolName}", toolName);
            return ToolExecutionResult.Fail($"Tool execution failed: {ex.Message}");
        }
    }

    public IReadOnlyList<object> GetToolDefinitions()
    {
        return
        [
            new
            {
                name = "search_knowledge",
                description = "Search the knowledge base for information about products, services, policies, or FAQs. Use this when the user asks questions that might be answered by existing knowledge articles.",
                input_schema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["query"] = new { type = "string", description = "Search query text" },
                        ["max_results"] = new { type = "integer", description = "Maximum results to return (default: 3)" }
                    },
                    required = new[] { "query" }
                }
            },
            new
            {
                name = "list_services",
                description = "List available services with their prices, durations, and descriptions. Use this when users ask about available services, pricing, or what is offered.",
                input_schema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["category"] = new { type = "string", description = "Filter by category (optional)" }
                    },
                    required = Array.Empty<string>()
                }
            },
            new
            {
                name = "check_availability",
                description = "Check available time slots for a specific service on a given date. Use this when users want to book an appointment or check availability.",
                input_schema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["service_id"] = new { type = "string", description = "The service ID (GUID)" },
                        ["date"] = new { type = "string", description = "Date in YYYY-MM-DD format" }
                    },
                    required = new[] { "service_id", "date" }
                }
            }
        ];
    }

    private async Task<ToolExecutionResult> ExecuteSearchKnowledgeAsync(JsonElement input, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var knowledgeMgr = scope.ServiceProvider.GetRequiredService<IKnowledgeManager>();

        var query = input.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
        var maxResults = input.TryGetProperty("max_results", out var m) ? m.GetInt32() : 3;

        var results = await knowledgeMgr.SearchAsync(new KnowledgeSearchRequest
        {
            Query = query,
            MaxResults = maxResults
        }, ct);

        if (results.Count == 0)
            return ToolExecutionResult.Ok("No knowledge base articles found matching the query.");

        var sb = new System.Text.StringBuilder();
        foreach (var r in results)
        {
            sb.AppendLine($"**{r.Entry.Title}** (score: {r.Score:F1})");
            sb.AppendLine(r.Entry.Content.Length > 500 ? r.Entry.Content[..500] + "..." : r.Entry.Content);
            sb.AppendLine();
        }

        return ToolExecutionResult.Ok(sb.ToString());
    }

    private async Task<ToolExecutionResult> ExecuteListServicesAsync(JsonElement input, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var serviceMgr = scope.ServiceProvider.GetRequiredService<IServiceManager>();

        var category = input.TryGetProperty("category", out var c) ? c.GetString() : null;
        var result = await serviceMgr.ListAsync(1, 50, category, ct);

        if (result.Items.Count == 0)
            return ToolExecutionResult.Ok("No services are currently available.");

        var sb = new System.Text.StringBuilder();
        foreach (var s in result.Items.Where(s => s.IsActive))
        {
            sb.AppendLine($"- **{s.Name}** ({s.Category}): ${s.Price:F2}, {s.DurationMinutes} min");
            if (!string.IsNullOrEmpty(s.Description))
                sb.AppendLine($"  {s.Description}");
        }

        return ToolExecutionResult.Ok(sb.ToString());
    }

    private async Task<ToolExecutionResult> ExecuteCheckAvailabilityAsync(JsonElement input, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var bookingMgr = scope.ServiceProvider.GetRequiredService<IBookingManager>();

        var serviceIdStr = input.TryGetProperty("service_id", out var sid) ? sid.GetString() ?? "" : "";
        var dateStr = input.TryGetProperty("date", out var d) ? d.GetString() ?? "" : "";

        if (!Guid.TryParse(serviceIdStr, out var serviceId))
            return ToolExecutionResult.Fail("Invalid service_id format. Must be a GUID.");

        if (!DateOnly.TryParse(dateStr, out var date))
            return ToolExecutionResult.Fail("Invalid date format. Use YYYY-MM-DD.");

        var slots = await bookingMgr.GetAvailableSlotsAsync(serviceId, date, null, ct);

        if (slots.Count == 0)
            return ToolExecutionResult.Ok($"No available slots on {date:yyyy-MM-dd}.");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Available slots on {date:yyyy-MM-dd}:");
        foreach (var slot in slots)
            sb.AppendLine($"- {slot.StartTime:HH:mm} to {slot.EndTime:HH:mm}");

        return ToolExecutionResult.Ok(sb.ToString());
    }
}
