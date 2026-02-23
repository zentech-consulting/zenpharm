using System.ComponentModel.DataAnnotations;

namespace Api.Features.AiChat;

public sealed record AiChatRequest
{
    [Required, MaxLength(4000)]
    public string Message { get; init; } = "";

    public IReadOnlyList<ChatMessage>? History { get; init; }

    public string? SessionToken { get; init; }
}

public sealed record ChatMessage(string Role, string Content);

public sealed record AiChatResponse(
    string Reply,
    string? Model,
    string? SessionToken,
    IReadOnlyList<ToolResultInfo>? ToolResults);

public sealed record ToolResultInfo(
    string ToolName,
    bool Success,
    string? Summary);

public sealed record StreamEvent
{
    public string Type { get; init; } = "";
    public string? Text { get; init; }
    public string? ToolName { get; init; }
    public string? Model { get; init; }
    public string? Error { get; init; }
    public ToolResultInfo? ToolResult { get; init; }
    public string? SessionToken { get; init; }
}
