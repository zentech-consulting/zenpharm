using System.Text.Json;

namespace Api.Features.AiChat.Tools;

internal sealed class AiToolExecutor(
    ILogger<AiToolExecutor> logger) : IAiToolExecutor
{
    public Task<ToolExecutionResult> ExecuteAsync(string toolName, JsonElement input, CancellationToken ct = default)
    {
        logger.LogInformation("Tool execution requested: {ToolName}", toolName);

        return Task.FromResult(
            ToolExecutionResult.Fail($"Tool '{toolName}' is not yet registered. Configure industry-specific tools in the tool executor."));
    }

    public IReadOnlyList<object> GetToolDefinitions()
    {
        // Industry-specific tools are registered here.
        // Each tool definition follows the Anthropic tool_use schema:
        //   { name, description, input_schema: { type, properties, required } }
        return [];
    }
}
