# SSE Streaming Implementation Reference

Complete implementation reference for adding Server-Sent Events streaming to an AI consultant. Based on production implementation in zentech-website (2026-02-12).

## Backend Files to Create/Modify

### 1. StreamEvent Contract (add to AiChatContracts.cs)

```csharp
public sealed record StreamEvent
{
    public string Type { get; init; } = "";
    public string? Text { get; init; }
    public string? ToolName { get; init; }
    public string? Model { get; init; }
    public string? Error { get; init; }
}
```

### 2. AnthropicRequest — Add Stream Property

```csharp
// Add to existing AnthropicRequest class
[JsonPropertyName("stream")]
public bool? Stream { get; init; }
```

### 3. AiChatManager — Add SendStreamAsync

```csharp
public IAsyncEnumerable<StreamEvent> SendStreamAsync(
    AiChatRequest request, CancellationToken ct = default)
{
    var channel = Channel.CreateUnbounded<StreamEvent>(new UnboundedChannelOptions
    {
        SingleWriter = true,
        SingleReader = true
    });
    _ = ProduceStreamEventsAsync(request, channel.Writer, ct);
    return channel.Reader.ReadAllAsync(ct);
}

private async Task ProduceStreamEventsAsync(
    AiChatRequest request,
    ChannelWriter<StreamEvent> writer,
    CancellationToken ct)
{
    try
    {
        // 1. Validation (reuse existing validation logic)
        // 2. Rate limiting (reuse existing)
        // 3. Build messages (reuse existing)

        var iteration = 0;
        var config = _configuration.GetSection("AiChat");
        var maxToolIterations = config.GetValue<int>("MaxToolIterations", 8);
        var model = config.GetValue<string>("Model") ?? "claude-sonnet-4-5";

        while (iteration < maxToolIterations)
        {
            iteration++;

            // Build Anthropic request WITH stream: true
            var anthropicRequest = new AnthropicRequest
            {
                Model = model,
                MaxTokens = config.GetValue<int>("MaxTokens", 2048),
                System = systemPrompt,
                Messages = messages,
                Tools = tools,
                Stream = true  // <-- This enables streaming
            };

            // Call Anthropic with streaming
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(anthropicRequest, _jsonOptions),
                    Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("x-api-key", apiKey);

            // CRITICAL: ResponseHeadersRead = don't buffer entire response
            // CRITICAL: using = prevent connection leak
            using var httpResponse = await _httpClient.SendAsync(
                httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content.ReadAsStringAsync(ct);
                await writer.WriteAsync(new StreamEvent
                {
                    Type = "error",
                    Error = $"API error: {httpResponse.StatusCode}"
                }, ct);
                break;
            }

            // Parse Anthropic SSE stream
            var stream = await httpResponse.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            string? sseEventType = null;
            string? stopReason = null;
            var textContent = new StringBuilder();
            var toolBlocks = new Dictionary<int, (string Id, string Name, StringBuilder Json)>();

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                if (line.StartsWith("event: "))
                {
                    sseEventType = line[7..];
                }
                else if (line.StartsWith("data: ") && sseEventType != null)
                {
                    var data = line[6..];
                    if (data == "[DONE]") break;

                    var json = JsonSerializer.Deserialize<JsonElement>(data);

                    switch (sseEventType)
                    {
                        case "content_block_start":
                        {
                            var block = json.GetProperty("content_block");
                            var blockType = block.GetProperty("type").GetString();
                            if (blockType == "tool_use")
                            {
                                var idx = json.GetProperty("index").GetInt32();
                                var id = block.GetProperty("id").GetString()!;
                                var name = block.GetProperty("name").GetString()!;
                                toolBlocks[idx] = (id, name, new StringBuilder());
                                await writer.WriteAsync(new StreamEvent
                                {
                                    Type = "tool_start", ToolName = name
                                }, ct);
                            }
                            break;
                        }
                        case "content_block_delta":
                        {
                            var delta = json.GetProperty("delta");
                            var deltaType = delta.GetProperty("type").GetString();
                            if (deltaType == "text_delta")
                            {
                                var text = delta.GetProperty("text").GetString();
                                if (!string.IsNullOrEmpty(text))
                                {
                                    textContent.Append(text);
                                    await writer.WriteAsync(new StreamEvent
                                    {
                                        Type = "text", Text = text
                                    }, ct);
                                }
                            }
                            else if (deltaType == "input_json_delta")
                            {
                                var idx = json.GetProperty("index").GetInt32();
                                var partial = delta.GetProperty("partial_json").GetString();
                                if (toolBlocks.TryGetValue(idx, out var tb))
                                    tb.Json.Append(partial);
                            }
                            break;
                        }
                        case "message_delta":
                        {
                            if (json.TryGetProperty("delta", out var d) &&
                                d.TryGetProperty("stop_reason", out var sr))
                            {
                                stopReason = sr.GetString();
                            }
                            break;
                        }
                    }
                    sseEventType = null;
                }
            }

            // Handle tool use
            if (stopReason == "tool_use" && toolBlocks.Count > 0)
            {
                // Build assistant message with text + tool_use blocks
                var contentBlocks = new List<object>();
                if (textContent.Length > 0)
                    contentBlocks.Add(new { type = "text", text = textContent.ToString() });

                foreach (var (idx, tb) in toolBlocks)
                {
                    var inputJson = JsonSerializer.Deserialize<JsonElement>(tb.Json.ToString());
                    contentBlocks.Add(new { type = "tool_use", id = tb.Id, name = tb.Name, input = inputJson });
                }

                messages.Add(new AnthropicMessage { Role = "assistant", ContentBlocks = contentBlocks });

                // Execute tools and build results
                var toolResults = new List<object>();
                foreach (var (idx, tb) in toolBlocks)
                {
                    var inputJson = JsonSerializer.Deserialize<JsonElement>(tb.Json.ToString());
                    var result = await _toolExecutor.ExecuteAsync(tb.Name, inputJson, ct);
                    await writer.WriteAsync(new StreamEvent { Type = "tool_end", ToolName = tb.Name }, ct);
                    toolResults.Add(new
                    {
                        type = "tool_result",
                        tool_use_id = tb.Id,
                        content = result.Success ? result.Data : result.Error,
                        is_error = !result.Success ? true : (bool?)null
                    });
                }

                messages.Add(new AnthropicMessage { Role = "user", ContentBlocks = toolResults });

                // Reset for next iteration
                textContent.Clear();
                toolBlocks.Clear();
            }
            else
            {
                break; // No tool use, streaming is done
            }
        }

        await writer.WriteAsync(new StreamEvent { Type = "done", Model = model }, ct);
    }
    catch (OperationCanceledException) { /* Client disconnected */ }
    catch (Exception ex)
    {
        try { await writer.WriteAsync(new StreamEvent { Type = "error", Error = "An error occurred" }, ct); }
        catch { /* Channel may already be closed */ }
    }
    finally
    {
        writer.Complete();
    }
}
```

### 4. AiChatEndpoints — Add Streaming Endpoint

```csharp
g.MapPost("/stream", async (
    AiChatRequest request,
    IAiChatManager chatManager,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    // Same validation as non-streaming endpoint
    if (string.IsNullOrWhiteSpace(request.Message))
        return Results.BadRequest(new { error = "Message is required" });
    if (request.Message.Length > 2000)
        return Results.BadRequest(new { error = "Message too long (max 2000)" });

    // Set client IP — extract to shared method to avoid DRY violation with non-streaming endpoint
    chatManager.ClientIp = ResolveClientIp(httpContext);

    // SSE headers — all three are critical
    httpContext.Response.ContentType = "text/event-stream";
    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers["X-Accel-Buffering"] = "no";

    var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    await foreach (var evt in chatManager.SendStreamAsync(request, ct))
    {
        var json = JsonSerializer.Serialize(evt, jsonOptions);
        await httpContext.Response.WriteAsync($"event: {evt.Type}\ndata: {json}\n\n", ct);
        await httpContext.Response.Body.FlushAsync(ct);
    }

    // NOTE: No return — void endpoint, we already wrote to the response stream
})
.AllowAnonymous();

// Shared helper — used by both /api/ai-chat and /api/ai-chat/stream
static string ResolveClientIp(HttpContext httpContext)
{
    var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrEmpty(forwarded))
    {
        var ip = forwarded.Split(',')[0].Trim();
        if (System.Net.IPAddress.TryParse(ip, out _)) return ip;
    }
    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
```

## Frontend Files to Create/Modify

### 1. API Client — Add streamChatMessage (chat.ts)

```typescript
export type StreamEvent = {
  type: "text" | "tool_start" | "tool_end" | "done" | "error";
  text?: string;
  toolName?: string;
  model?: string;
  error?: string;
};

export async function streamChatMessage(
  request: AiChatRequest,
  onEvent: (event: StreamEvent) => void,
  signal?: AbortSignal  // <-- Support cancellation
): Promise<void> {
  const res = await fetch("/api/ai-chat/stream", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
    signal,  // <-- Pass AbortSignal to fetch
  });

  if (!res.ok) {
    if (res.status === 429) throw new Error("Too many requests. Please wait.");
    throw new Error("Failed to send message");
  }

  if (!res.body) throw new Error("Streaming not supported");  // <-- Null guard
  const reader = res.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split("\n");
    buffer = lines.pop() || "";

    let currentEventType = "";
    for (const line of lines) {
      if (line.startsWith("event: ")) {
        currentEventType = line.slice(7);
      } else if (line.startsWith("data: ") && currentEventType) {
        try {
          onEvent(JSON.parse(line.slice(6)));
        } catch { /* ignore */ }
        currentEventType = "";
      }
    }
  }
}
```

### 2. Chat Component — Token-by-Token Rendering

Replace the existing `sendMessage` function with streaming version:

```typescript
// Tool display names — customize per project
const toolDisplayNames: Record<string, string> = {
  // Add your tool names here
};

const [toolStatus, setToolStatus] = useState<string | null>(null);

const handleSendMessage = useCallback(async (userMessage: string) => {
  const userMsg: ChatMessage = { role: "user", content: userMessage };
  const newMessages = [...messages, userMsg];
  const newHistory = [...history, userMsg];

  setMessages(newMessages);
  setIsLoading(true);
  setError(null);

  try {
    await streamChatMessage(
      { message: userMessage, history: newHistory.slice(-20) },
      (event) => {
        switch (event.type) {
          case "text":
            if (event.text) {
              setMessages(prev => {
                const last = prev[prev.length - 1];
                if (last?.role === "assistant") {
                  const updated = [...prev];
                  updated[updated.length - 1] = {
                    ...last,
                    content: last.content + event.text,
                  };
                  return updated;
                }
                return [...prev, { role: "assistant", content: event.text! }];
              });
            }
            break;
          case "tool_start":
            setToolStatus(toolDisplayNames[event.toolName || ""] || "Processing...");
            break;
          case "tool_end":
            setToolStatus(null);
            break;
          case "error":
            setError(event.error || "Something went wrong");
            break;
        }
      }
    );

    // After stream completes, update history with final assistant message
    setMessages(prev => {
      const lastAssistant = prev.findLast(m => m.role === "assistant");
      if (lastAssistant) {
        setHistory(h => [...h, userMsg, lastAssistant]);
        saveHistory([...newHistory, lastAssistant]);
      }
      return prev;
    });
  } catch (err) {
    setError(err instanceof Error ? err.message : "Something went wrong");
  } finally {
    setIsLoading(false);
    setToolStatus(null);
  }
}, [messages, history]);
```

### 3. Tool Status UI Element

```tsx
{/* Show during tool execution */}
{toolStatus && (
  <div className="flex items-center gap-2 text-sm text-gray-400 px-4 py-2">
    <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
      <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" opacity="0.25" />
      <path fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
    </svg>
    {toolStatus}
  </div>
)}
```

## Code Review Checklist (from SMCP production)

These issues were caught during SMCP's SSE implementation code review:

### Backend
- [ ] `httpResponse` wrapped in `using` — prevents connection leak under high load
- [ ] `ResolveClientIp()` extracted as shared method — used by both endpoints
- [ ] `BuildMessages()` extracted as shared method — history validation logic in one place
- [ ] Tool JSON parse wrapped in try-catch — malformed input doesn't crash the stream
- [ ] Streaming endpoint returns void — no `Results.Empty` after writing to response stream

### Frontend
- [ ] `res.body` null guard before `.getReader()`
- [ ] `AbortSignal` parameter on `streamChatMessage()`
- [ ] `AbortController` in component — cancel previous request on new send or unmount

### AbortController Pattern (Frontend)

```typescript
// In your chat component
const abortControllerRef = useRef<AbortController | null>(null);

const handleSend = useCallback(async (message: string) => {
  // Cancel any in-flight request
  abortControllerRef.current?.abort();
  const controller = new AbortController();
  abortControllerRef.current = controller;

  try {
    await streamChatMessage(
      { message, history },
      onEvent,
      controller.signal  // <-- Pass signal
    );
  } catch (err) {
    if (err instanceof DOMException && err.name === "AbortError") return; // User cancelled
    setError(err instanceof Error ? err.message : "Something went wrong");
  }
}, [history]);

// Cleanup on unmount
useEffect(() => {
  return () => abortControllerRef.current?.abort();
}, []);
```

### BuildMessages Shared Method (Backend)

```csharp
// Extract from AiChatManager — used by both SendAsync and SendStreamAsync
private List<AnthropicMessage> BuildMessages(AiChatRequest request, string systemPrompt)
{
    var messages = new List<AnthropicMessage>();

    if (request.History is { Count: > 0 })
    {
        var totalLength = 0;
        var count = 0;
        foreach (var msg in request.History)
        {
            if (msg.Role is not ("user" or "assistant")) continue;
            var content = msg.Content?.Length > 2000 ? msg.Content[..2000] : msg.Content ?? "";
            totalLength += content.Length;
            if (totalLength > 20000 || count >= 20) break;
            messages.Add(new AnthropicMessage { Role = msg.Role, SimpleContent = content });
            count++;
        }
    }

    messages.Add(new AnthropicMessage { Role = "user", SimpleContent = request.Message });
    return messages;
}
```

## Required NuGet Packages

No additional packages needed. Uses:
- `System.Threading.Channels` (built into .NET 8)
- `System.Net.Http.HttpClient` (already registered)
- `System.Text.Json` (already used)

## Vite Proxy

No changes needed. Standard Vite proxy configuration passes SSE correctly:

```typescript
proxy: {
  "/api": {
    target: "http://localhost:YOUR_PORT",
    changeOrigin: true,
  },
}
```

## Testing Checklist

1. [ ] Text streams token-by-token (not in batches)
2. [ ] Tool execution shows status indicator, then resumes streaming
3. [ ] Multiple tool calls in one response work correctly
4. [ ] Error during streaming shows error message
5. [ ] Client disconnect cancels backend processing (CancellationToken)
6. [ ] Rate limiting works on streaming endpoint
7. [ ] DryRun mode works with streaming
8. [ ] Long conversations (20+ messages history) stream correctly
9. [ ] SSE works through Vite dev proxy
10. [ ] SSE works through production reverse proxy (Nginx/Azure)
