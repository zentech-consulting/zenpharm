using System.Text.Json;
using Api.Features.Bookings;
using Api.Features.Knowledge;
using Api.Features.MasterProducts;
using Api.Features.Products;
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
                "search_products" => await ExecuteSearchProductsAsync(input, ct),
                "check_stock" => await ExecuteCheckStockAsync(input, ct),
                "medication_info" => await ExecuteMedicationInfoAsync(input, ct),
                _ => ToolExecutionResult.Fail($"Unknown tool: {toolName}")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tool execution failed: {ToolName}", toolName);
            return ToolExecutionResult.Fail("An internal error occurred while executing this tool. Please try again later.");
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
            },
            new
            {
                name = "search_products",
                description = "Search the tenant's product catalogue by name, category, or schedule class. Returns product details including stock levels and pricing. Use when customers ask about available products, prices, or product availability.",
                input_schema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["query"] = new { type = "string", description = "Search text (product name, brand, or category)" },
                        ["schedule_class"] = new { type = "string", description = "Filter by schedule class: Unscheduled, S2, S3, S4 (optional)" },
                        ["max_results"] = new { type = "integer", description = "Maximum results to return (default: 10)" }
                    },
                    required = new[] { "query" }
                }
            },
            new
            {
                name = "check_stock",
                description = "Check the current stock level and availability for a specific product. Returns stock quantity, reorder level, expiry date, and pricing. Use when customers ask if a product is in stock or available.",
                input_schema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["product_name"] = new { type = "string", description = "The product name to look up" }
                    },
                    required = new[] { "product_name" }
                }
            },
            new
            {
                name = "medication_info",
                description = "Get detailed medication information from the master product catalogue including active ingredients, warnings, schedule class, and pack sizes. Use when customers ask about drug interactions, ingredients, side effects, or safety information.",
                input_schema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["medication_name"] = new { type = "string", description = "The medication or product name to look up" }
                    },
                    required = new[] { "medication_name" }
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

    // ================================================================
    // Pharmacy-Specific Tools
    // ================================================================

    private async Task<ToolExecutionResult> ExecuteSearchProductsAsync(JsonElement input, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var productMgr = scope.ServiceProvider.GetRequiredService<IProductManager>();

        var query = input.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
        var maxResults = input.TryGetProperty("max_results", out var m) ? m.GetInt32() : 10;

        var result = await productMgr.ListAsync(1, maxResults, query, false, false, ct);

        if (result.Items.Count == 0)
            return ToolExecutionResult.Ok($"No products found matching '{query}'.");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Found {result.TotalCount} product(s) matching '{query}':");
        sb.AppendLine();

        foreach (var p in result.Items)
        {
            var displayName = p.CustomName ?? p.MasterProductName;
            var price = p.CustomPrice ?? p.DefaultPrice;
            var stockStatus = p.StockQuantity <= p.ReorderLevel ? "LOW STOCK" : "In Stock";

            sb.AppendLine($"- **{displayName}** ({p.Brand ?? "Generic"})");
            sb.AppendLine($"  Category: {p.Category} | Schedule: {p.ScheduleClass} | Price: ${price:F2}");
            sb.AppendLine($"  Stock: {p.StockQuantity} ({stockStatus})");
            if (p.ExpiryDate.HasValue)
                sb.AppendLine($"  Expiry: {p.ExpiryDate.Value:yyyy-MM-dd}");
            sb.AppendLine();
        }

        return ToolExecutionResult.Ok(sb.ToString());
    }

    private async Task<ToolExecutionResult> ExecuteCheckStockAsync(JsonElement input, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var productMgr = scope.ServiceProvider.GetRequiredService<IProductManager>();

        var productName = input.TryGetProperty("product_name", out var pn) ? pn.GetString() ?? "" : "";

        var result = await productMgr.ListAsync(1, 5, productName, false, false, ct);

        if (result.Items.Count == 0)
            return ToolExecutionResult.Ok($"Product '{productName}' was not found in our inventory.");

        var p = result.Items[0];
        var displayName = p.CustomName ?? p.MasterProductName;
        var price = p.CustomPrice ?? p.DefaultPrice;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"**{displayName}**");
        sb.AppendLine($"- Brand: {p.Brand ?? "Generic"}");
        sb.AppendLine($"- Category: {p.Category}");
        sb.AppendLine($"- Schedule Class: {p.ScheduleClass}");
        sb.AppendLine($"- Price: ${price:F2}");
        sb.AppendLine($"- Current Stock: {p.StockQuantity} units");
        sb.AppendLine($"- Reorder Level: {p.ReorderLevel}");

        if (p.StockQuantity <= 0)
            sb.AppendLine($"- **Status: OUT OF STOCK**");
        else if (p.StockQuantity <= p.ReorderLevel)
            sb.AppendLine($"- **Status: LOW STOCK** — only {p.StockQuantity} remaining");
        else
            sb.AppendLine($"- Status: In Stock");

        if (p.ExpiryDate.HasValue)
            sb.AppendLine($"- Expiry Date: {p.ExpiryDate.Value:yyyy-MM-dd}");

        if (p.ScheduleClass == "S3")
            sb.AppendLine($"- Note: This is a Pharmacist Only (S3) medicine — available over the counter with pharmacist consultation.");
        else if (p.ScheduleClass == "S4")
            sb.AppendLine($"- Note: This is a Prescription Only (S4) medicine — a valid prescription is required.");

        return ToolExecutionResult.Ok(sb.ToString());
    }

    private async Task<ToolExecutionResult> ExecuteMedicationInfoAsync(JsonElement input, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var masterProductMgr = scope.ServiceProvider.GetRequiredService<IMasterProductManager>();

        var medicationName = input.TryGetProperty("medication_name", out var mn) ? mn.GetString() ?? "" : "";

        var result = await masterProductMgr.ListAsync(1, 5, null, medicationName, null, ct);

        if (result.Items.Count == 0)
            return ToolExecutionResult.Ok($"No medication information found for '{medicationName}'.");

        var med = result.Items[0];
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"**{med.Name}**");

        if (!string.IsNullOrEmpty(med.GenericName))
            sb.AppendLine($"- Generic Name: {med.GenericName}");
        if (!string.IsNullOrEmpty(med.Brand))
            sb.AppendLine($"- Brand: {med.Brand}");

        sb.AppendLine($"- Schedule Class: {med.ScheduleClass}");
        sb.AppendLine($"- Category: {med.Category}");

        if (!string.IsNullOrEmpty(med.ActiveIngredients))
            sb.AppendLine($"- Active Ingredients: {med.ActiveIngredients}");
        if (!string.IsNullOrEmpty(med.PackSize))
            sb.AppendLine($"- Pack Size: {med.PackSize}");
        if (!string.IsNullOrEmpty(med.Description))
            sb.AppendLine($"- Description: {med.Description}");
        if (!string.IsNullOrEmpty(med.Warnings))
            sb.AppendLine($"- Warnings: {med.Warnings}");
        if (!string.IsNullOrEmpty(med.PbsItemCode))
            sb.AppendLine($"- PBS Item Code: {med.PbsItemCode}");

        sb.AppendLine();
        sb.AppendLine("*This information is for general reference only. Always consult your pharmacist or doctor for personalised medical advice.*");

        return ToolExecutionResult.Ok(sb.ToString());
    }
}
