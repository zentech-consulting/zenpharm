using Api.Features.AiChat;
using Api.Features.AiChat.Tools;
using Xunit;

namespace Api.Tests.AiChat;

public class AiChatTests
{
    // --- Contract Default Tests ---

    [Fact]
    public void AiChatRequest_DefaultValues()
    {
        var req = new AiChatRequest();

        Assert.Equal("", req.Message);
        Assert.Null(req.History);
        Assert.Null(req.SessionToken);
    }

    [Fact]
    public void ChatMessage_RecordEquality()
    {
        var a = new ChatMessage("user", "Hello");
        var b = new ChatMessage("user", "Hello");

        Assert.Equal(a, b);
    }

    [Fact]
    public void AiChatResponse_RecordEquality()
    {
        var a = new AiChatResponse("Reply", "model-1", "session-abc", null);
        var b = new AiChatResponse("Reply", "model-1", "session-abc", null);

        Assert.Equal(a, b);
    }

    [Fact]
    public void ToolResultInfo_RecordEquality()
    {
        var a = new ToolResultInfo("search_knowledge", true, "Found 3 results");
        var b = new ToolResultInfo("search_knowledge", true, "Found 3 results");

        Assert.Equal(a, b);
    }

    [Fact]
    public void StreamEvent_DefaultValues()
    {
        var evt = new StreamEvent();

        Assert.Equal("", evt.Type);
        Assert.Null(evt.Text);
        Assert.Null(evt.ToolName);
        Assert.Null(evt.Model);
        Assert.Null(evt.Error);
        Assert.Null(evt.ToolResult);
        Assert.Null(evt.SessionToken);
    }

    [Fact]
    public void StreamEvent_TextType()
    {
        var evt = new StreamEvent { Type = "text", Text = "Hello world" };

        Assert.Equal("text", evt.Type);
        Assert.Equal("Hello world", evt.Text);
    }

    [Fact]
    public void StreamEvent_DoneType()
    {
        var evt = new StreamEvent { Type = "done", Model = "claude-sonnet-4-5-20250929", SessionToken = "abc123" };

        Assert.Equal("done", evt.Type);
        Assert.Equal("claude-sonnet-4-5-20250929", evt.Model);
        Assert.Equal("abc123", evt.SessionToken);
    }

    [Fact]
    public void StreamEvent_ErrorType()
    {
        var evt = new StreamEvent { Type = "error", Error = "Rate limit exceeded" };

        Assert.Equal("error", evt.Type);
        Assert.Equal("Rate limit exceeded", evt.Error);
    }

    [Fact]
    public void StreamEvent_ToolTypes()
    {
        var startEvt = new StreamEvent { Type = "tool_start", ToolName = "search_knowledge" };
        var resultEvt = new StreamEvent
        {
            Type = "tool_result",
            ToolResult = new ToolResultInfo("search_knowledge", true, "Found 2 articles")
        };

        Assert.Equal("tool_start", startEvt.Type);
        Assert.Equal("search_knowledge", startEvt.ToolName);
        Assert.Equal("tool_result", resultEvt.Type);
        Assert.True(resultEvt.ToolResult!.Success);
    }

    // --- ToolExecutionResult Tests ---

    [Fact]
    public void ToolExecutionResult_Ok()
    {
        var result = ToolExecutionResult.Ok("Some content");

        Assert.True(result.Success);
        Assert.Equal("Some content", result.Content);
    }

    [Fact]
    public void ToolExecutionResult_Fail()
    {
        var result = ToolExecutionResult.Fail("Not found");

        Assert.False(result.Success);
        Assert.Equal("Not found", result.Content);
    }

    // --- MaskPii Tests ---

    [Fact]
    public void MaskPii_MasksEmail()
    {
        var result = AiChatManager.MaskPii("Contact user@example.com for details.");

        Assert.Contains("[EMAIL]", result);
        Assert.DoesNotContain("user@example.com", result);
    }

    [Fact]
    public void MaskPii_MasksAustralianPhone()
    {
        var result = AiChatManager.MaskPii("Call me at 0412 345 678 please.");

        Assert.Contains("[PHONE]", result);
        Assert.DoesNotContain("0412 345 678", result);
    }

    [Fact]
    public void MaskPii_MasksInternationalPhone()
    {
        var result = AiChatManager.MaskPii("My number is +61412345678.");

        Assert.Contains("[PHONE]", result);
        Assert.DoesNotContain("+61412345678", result);
    }

    [Fact]
    public void MaskPii_PreservesNormalText()
    {
        var input = "Hello, how can I help you today?";
        var result = AiChatManager.MaskPii(input);

        Assert.Equal(input, result);
    }

    // --- BuildMessages Tests ---

    [Fact]
    public void BuildMessages_NoHistory_SingleUserMessage()
    {
        var request = new AiChatRequest { Message = "Hello" };
        var messages = AiChatManager.BuildMessages(request);

        Assert.Single(messages);
        Assert.Equal("user", messages[0].Role);
        Assert.Equal("Hello", messages[0].Content);
    }

    [Fact]
    public void BuildMessages_WithHistory_PreservesOrder()
    {
        var request = new AiChatRequest
        {
            Message = "Follow up",
            History =
            [
                new ChatMessage("user", "First message"),
                new ChatMessage("assistant", "First reply")
            ]
        };

        var messages = AiChatManager.BuildMessages(request);

        Assert.Equal(3, messages.Count);
        Assert.Equal("user", messages[0].Role);
        Assert.Equal("First message", messages[0].Content);
        Assert.Equal("assistant", messages[1].Role);
        Assert.Equal("First reply", messages[1].Content);
        Assert.Equal("user", messages[2].Role);
        Assert.Equal("Follow up", messages[2].Content);
    }

    [Fact]
    public void BuildMessages_TruncatesLongMessages()
    {
        var longMessage = new string('x', 3000);
        var request = new AiChatRequest
        {
            Message = "Current",
            History = [new ChatMessage("user", longMessage)]
        };

        var messages = AiChatManager.BuildMessages(request);

        Assert.Equal(2, messages.Count);
        Assert.Equal(2000, ((string)messages[0].Content).Length);
    }

    [Fact]
    public void BuildMessages_LimitsHistoryTo20()
    {
        var history = Enumerable.Range(0, 25)
            .Select(i => new ChatMessage("user", $"Message {i}"))
            .ToList();

        var request = new AiChatRequest
        {
            Message = "Current",
            History = history
        };

        var messages = AiChatManager.BuildMessages(request);

        // 20 from history + 1 current = 21
        Assert.Equal(21, messages.Count);
        // Should keep the last 20 from history
        Assert.Equal("Message 5", messages[0].Content);
    }

    [Fact]
    public void BuildMessages_InvalidRole_DefaultsToUser()
    {
        var request = new AiChatRequest
        {
            Message = "Current",
            History = [new ChatMessage("system", "System msg")]
        };

        var messages = AiChatManager.BuildMessages(request);

        Assert.Equal(2, messages.Count);
        Assert.Equal("user", messages[0].Role);
    }

    // --- Anthropic Contract Tests ---

    [Fact]
    public void AnthropicContentBlock_DefaultValues()
    {
        var block = new AnthropicContentBlock();

        Assert.Equal("", block.Type);
        Assert.Null(block.Text);
        Assert.Null(block.Id);
        Assert.Null(block.Name);
        Assert.Null(block.Input);
    }

    [Fact]
    public void AnthropicResponse_DefaultValues()
    {
        var response = new AnthropicResponse();

        Assert.Equal("", response.Id);
        Assert.Equal("", response.Model);
        Assert.Null(response.StopReason);
        Assert.Empty(response.Content);
    }
}
