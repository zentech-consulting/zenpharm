using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Api.Features.AiChat;

// --- Public API contracts ---

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

// --- Internal Anthropic API models ---

internal sealed record AnthropicRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = "";

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; init; } = 4096;

    [JsonPropertyName("system")]
    public string? System { get; init; }

    [JsonPropertyName("messages")]
    public IReadOnlyList<AnthropicMessage> Messages { get; init; } = [];

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<object>? Tools { get; init; }

    [JsonPropertyName("stream")]
    public bool Stream { get; init; }
}

internal sealed record AnthropicMessage
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = "";

    [JsonPropertyName("content")]
    public object Content { get; init; } = "";
}

internal sealed record AnthropicResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("model")]
    public string Model { get; init; } = "";

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; init; }

    [JsonPropertyName("content")]
    public IReadOnlyList<AnthropicContentBlock> Content { get; init; } = [];
}

internal sealed record AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("input")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public System.Text.Json.JsonElement? Input { get; init; }
}
