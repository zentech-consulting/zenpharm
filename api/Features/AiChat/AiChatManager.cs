using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Api.Common;
using Api.Features.AiChat.Tools;
using Dapper;

namespace Api.Features.AiChat;

internal sealed partial class AiChatManager(
    ITenantDb db,
    IConfiguration cfg,
    IHttpClientFactory httpClientFactory,
    IAiToolExecutor toolExecutor,
    ILogger<AiChatManager> logger) : IAiChatManager
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private const int MaxHistoryMessages = 20;
    private const int MaxMessageLength = 2000;

    public async Task<AiChatResponse> SendAsync(AiChatRequest request, string? clientIp, CancellationToken ct = default)
    {
        var dryRun = cfg.GetValue<bool>("AiChat:DryRun");
        if (dryRun)
        {
            logger.LogInformation("AiChat DryRun mode. MessageLength={Length}", request.Message?.Length ?? 0);
            return new AiChatResponse(
                Reply: "This is a dry-run response. AI chat is not yet connected.",
                Model: "dry-run",
                SessionToken: Guid.NewGuid().ToString("N")[..12],
                ToolResults: null);
        }

        var apiKey = cfg["AiChat:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("AiChat:ApiKey is not configured");

        var model = cfg["AiChat:Model"] ?? "claude-sonnet-4-5-20250929";
        var maxTokens = cfg.GetValue("AiChat:MaxTokens", 4096);
        var systemPrompt = cfg["AiChat:SystemPrompt"] ?? "You are a helpful AI consultant.";
        var maxIterations = cfg.GetValue("AiChat:MaxToolIterations", 5);

        var messages = BuildMessages(request);
        var tools = toolExecutor.GetToolDefinitions();
        var toolResults = new List<ToolResultInfo>();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var anthropicRequest = new AnthropicRequest
            {
                Model = model,
                MaxTokens = maxTokens,
                System = systemPrompt,
                Messages = messages,
                Tools = tools.Count > 0 ? tools : null,
                Stream = false
            };

            var response = await CallAnthropicAsync(apiKey, anthropicRequest, ct);

            var textParts = new StringBuilder();
            var hasToolUse = false;

            foreach (var block in response.Content)
            {
                if (block.Type == "text" && block.Text is not null)
                {
                    textParts.Append(block.Text);
                }
                else if (block.Type == "tool_use" && block.Name is not null && block.Id is not null)
                {
                    hasToolUse = true;
                    logger.LogInformation("Tool call: {ToolName}", block.Name);

                    var toolResult = await toolExecutor.ExecuteAsync(
                        block.Name, block.Input ?? default, ct);

                    toolResults.Add(new ToolResultInfo(block.Name, toolResult.Success, toolResult.Content));

                    // Append assistant message with tool_use, then tool_result
                    messages = [..messages,
                        new AnthropicMessage
                        {
                            Role = "assistant",
                            Content = JsonSerializer.SerializeToElement(response.Content, s_jsonOptions)
                        },
                        new AnthropicMessage
                        {
                            Role = "user",
                            Content = JsonSerializer.SerializeToElement(new[]
                            {
                                new { type = "tool_result", tool_use_id = block.Id, content = toolResult.Content }
                            }, s_jsonOptions)
                        }
                    ];
                }
            }

            if (!hasToolUse || response.StopReason != "tool_use")
            {
                sw.Stop();
                var reply = MaskPii(textParts.ToString());
                var sessionToken = request.SessionToken ?? Guid.NewGuid().ToString("N")[..12];

                _ = PersistConversationAsync(sessionToken, clientIp, request.Message, reply, sw.ElapsedMilliseconds);

                return new AiChatResponse(reply, model, sessionToken,
                    toolResults.Count > 0 ? toolResults : null);
            }
        }

        return new AiChatResponse(
            "I've reached the maximum number of tool calls. Please try rephrasing your question.",
            model, request.SessionToken, toolResults.Count > 0 ? toolResults : null);
    }

    public async IAsyncEnumerable<StreamEvent> SendStreamAsync(
        AiChatRequest request,
        string? clientIp,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var dryRun = cfg.GetValue<bool>("AiChat:DryRun");
        if (dryRun)
        {
            logger.LogInformation("AiChat DryRun stream mode. MessageLength={Length}", request.Message?.Length ?? 0);
            yield return new StreamEvent
            {
                Type = "text",
                Text = "This is a dry-run response. AI chat is not yet connected."
            };
            yield return new StreamEvent
            {
                Type = "done",
                Model = "dry-run",
                SessionToken = Guid.NewGuid().ToString("N")[..12]
            };
            yield break;
        }

        var apiKey = cfg["AiChat:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            yield return new StreamEvent { Type = "error", Error = "AI chat is not configured" };
            yield break;
        }

        var model = cfg["AiChat:Model"] ?? "claude-sonnet-4-5-20250929";
        var maxTokens = cfg.GetValue("AiChat:MaxTokens", 4096);
        var systemPrompt = cfg["AiChat:SystemPrompt"] ?? "You are a helpful AI consultant.";
        var maxIterations = cfg.GetValue("AiChat:MaxToolIterations", 5);

        var messages = BuildMessages(request);
        var tools = toolExecutor.GetToolDefinitions();
        var fullReply = new StringBuilder();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var anthropicRequest = new AnthropicRequest
            {
                Model = model,
                MaxTokens = maxTokens,
                System = systemPrompt,
                Messages = messages,
                Tools = tools.Count > 0 ? tools : null,
                Stream = true
            };

            var client = httpClientFactory.CreateClient("Anthropic");
            client.DefaultRequestHeaders.Remove("x-api-key");
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);

            var json = JsonSerializer.Serialize(anthropicRequest, s_jsonOptions);
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var httpResp = await client.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, ct);
            httpResp.EnsureSuccessStatusCode();

            using var stream = await httpResp.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            var currentToolName = (string?)null;
            var toolInputJson = new StringBuilder();
            var toolUseId = (string?)null;
            var hasToolUse = false;
            var stopReason = (string?)null;

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line is null) break;

                if (!line.StartsWith("data: ")) continue;
                var data = line[6..];
                if (data == "[DONE]") break;

                // Parse SSE event outside yield context to avoid CS1626
                var parsed = ParseSseEvent(data, ref currentToolName, ref toolUseId,
                    toolInputJson, ref hasToolUse, ref stopReason, fullReply);

                // Yield events outside try-catch
                if (parsed.EmitEvent is not null)
                    yield return parsed.EmitEvent;

                // Handle tool completion
                if (parsed.ToolCompleted && currentToolName is null && parsed.CompletedToolName is not null && parsed.CompletedToolUseId is not null)
                {
                    var inputElement = toolInputJson.Length > 0
                        ? JsonSerializer.Deserialize<JsonElement>(toolInputJson.ToString())
                        : default;

                    var toolResult = await toolExecutor.ExecuteAsync(parsed.CompletedToolName, inputElement, ct);

                    yield return new StreamEvent
                    {
                        Type = "tool_result",
                        ToolResult = new ToolResultInfo(parsed.CompletedToolName, toolResult.Success, toolResult.Content)
                    };

                    var assistantContent = new List<object>();
                    if (fullReply.Length > 0)
                        assistantContent.Add(new { type = "text", text = fullReply.ToString() });
                    assistantContent.Add(new { type = "tool_use", id = parsed.CompletedToolUseId, name = parsed.CompletedToolName, input = inputElement });

                    messages = [..messages,
                        new AnthropicMessage
                        {
                            Role = "assistant",
                            Content = JsonSerializer.SerializeToElement(assistantContent, s_jsonOptions)
                        },
                        new AnthropicMessage
                        {
                            Role = "user",
                            Content = JsonSerializer.SerializeToElement(new[]
                            {
                                new { type = "tool_result", tool_use_id = parsed.CompletedToolUseId, content = toolResult.Content }
                            }, s_jsonOptions)
                        }
                    ];

                    fullReply.Clear();
                }
            }

            if (!hasToolUse || stopReason != "tool_use")
                break;
        }

        sw.Stop();
        var sessionToken = request.SessionToken ?? Guid.NewGuid().ToString("N")[..12];

        _ = PersistConversationAsync(sessionToken, clientIp, request.Message,
            MaskPii(fullReply.ToString()), sw.ElapsedMilliseconds);

        yield return new StreamEvent
        {
            Type = "done",
            Model = model,
            SessionToken = sessionToken
        };
    }

    internal static IReadOnlyList<AnthropicMessage> BuildMessages(AiChatRequest request)
    {
        var messages = new List<AnthropicMessage>();

        if (request.History is { Count: > 0 })
        {
            var history = request.History.Count > MaxHistoryMessages
                ? request.History.Skip(request.History.Count - MaxHistoryMessages).ToList()
                : request.History;

            foreach (var msg in history)
            {
                var content = msg.Content.Length > MaxMessageLength
                    ? msg.Content[..MaxMessageLength]
                    : msg.Content;

                var role = msg.Role.ToLowerInvariant() switch
                {
                    "user" => "user",
                    "assistant" => "assistant",
                    _ => "user"
                };

                messages.Add(new AnthropicMessage { Role = role, Content = content });
            }
        }

        messages.Add(new AnthropicMessage { Role = "user", Content = request.Message });
        return messages;
    }

    internal static string MaskPii(string text)
    {
        // Mask email addresses
        text = EmailRegex().Replace(text, "[EMAIL]");

        // Mask Australian phone numbers (04xx xxx xxx or +614xx xxx xxx)
        text = AustralianPhoneRegex().Replace(text, "[PHONE]");

        return text;
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?:\+?61|0)4\d{2}[\s-]?\d{3}[\s-]?\d{3}")]
    private static partial Regex AustralianPhoneRegex();

    private record struct SseParseResult(
        StreamEvent? EmitEvent,
        bool ToolCompleted,
        string? CompletedToolName,
        string? CompletedToolUseId);

    private static SseParseResult ParseSseEvent(
        string data,
        ref string? currentToolName,
        ref string? toolUseId,
        StringBuilder toolInputJson,
        ref bool hasToolUse,
        ref string? stopReason,
        StringBuilder fullReply)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;
            var eventType = root.GetProperty("type").GetString();

            switch (eventType)
            {
                case "content_block_start":
                    var blockType = root.GetProperty("content_block").GetProperty("type").GetString();
                    if (blockType == "tool_use")
                    {
                        currentToolName = root.GetProperty("content_block").GetProperty("name").GetString();
                        toolUseId = root.GetProperty("content_block").GetProperty("id").GetString();
                        toolInputJson.Clear();
                        hasToolUse = true;
                        return new SseParseResult(
                            new StreamEvent { Type = "tool_start", ToolName = currentToolName },
                            false, null, null);
                    }
                    break;

                case "content_block_delta":
                    var deltaType = root.GetProperty("delta").GetProperty("type").GetString();
                    if (deltaType == "text_delta")
                    {
                        var text = root.GetProperty("delta").GetProperty("text").GetString();
                        if (text is not null)
                        {
                            fullReply.Append(text);
                            return new SseParseResult(
                                new StreamEvent { Type = "text", Text = text },
                                false, null, null);
                        }
                    }
                    else if (deltaType == "input_json_delta")
                    {
                        var partial = root.GetProperty("delta").GetProperty("partial_json").GetString();
                        if (partial is not null)
                            toolInputJson.Append(partial);
                    }
                    break;

                case "content_block_stop":
                    if (currentToolName is not null && toolUseId is not null)
                    {
                        var completedName = currentToolName;
                        var completedId = toolUseId;
                        currentToolName = null;
                        toolUseId = null;
                        return new SseParseResult(null, true, completedName, completedId);
                    }
                    break;

                case "message_delta":
                    if (root.TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("stop_reason", out var sr))
                        stopReason = sr.GetString();
                    break;
            }
        }
        catch (JsonException)
        {
            // Skip malformed SSE lines
        }

        return default;
    }

    private async Task<AnthropicResponse> CallAnthropicAsync(
        string apiKey, AnthropicRequest request, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Anthropic");
        client.DefaultRequestHeaders.Remove("x-api-key");
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);

        var json = JsonSerializer.Serialize(request, s_jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/v1/messages", content, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<AnthropicResponse>(responseBody, s_jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialise Anthropic response");
    }

    private async Task PersistConversationAsync(
        string sessionToken, string? clientIp, string userMessage, string assistantReply, long durationMs)
    {
        try
        {
            using var conn = await db.CreateAsync();

            // Upsert session
            var sessionSql = """
                MERGE dbo.AiChatSessions AS target
                USING (SELECT @SessionToken AS SessionToken) AS source
                ON target.SessionToken = source.SessionToken
                WHEN MATCHED THEN
                    UPDATE SET MessageCount = MessageCount + 2, LastMessageAt = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (SessionToken, ClientIp, MessageCount)
                    VALUES (@SessionToken, @ClientIp, 2)
                OUTPUT INSERTED.Id;
                """;

            var sessionId = await conn.QuerySingleAsync<long>(sessionSql,
                new { SessionToken = sessionToken, ClientIp = clientIp });

            // Insert messages
            var msgSql = """
                INSERT INTO dbo.AiChatMessages (SessionId, Role, Content, DurationMs, Success)
                VALUES (@SessionId, @Role, @Content, @DurationMs, 1)
                """;

            await conn.ExecuteAsync(msgSql, new { SessionId = sessionId, Role = "user", Content = userMessage, DurationMs = (int?)null });
            await conn.ExecuteAsync(msgSql, new { SessionId = sessionId, Role = "assistant", Content = assistantReply, DurationMs = (int?)durationMs });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist AI conversation for session {SessionToken}", sessionToken);
        }
    }
}
