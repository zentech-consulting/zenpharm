using System.Text.Json;

namespace Api.Features.AiChat.Tools;

public interface IAiToolExecutor
{
    Task<ToolExecutionResult> ExecuteAsync(string toolName, JsonElement input, CancellationToken ct = default);
    IReadOnlyList<object> GetToolDefinitions();
}

public sealed record ToolExecutionResult(bool Success, string Content)
{
    public static ToolExecutionResult Ok(string content) => new(true, content);
    public static ToolExecutionResult Fail(string error) => new(false, error);
}
