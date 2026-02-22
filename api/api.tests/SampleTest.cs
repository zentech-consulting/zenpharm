using Api.Features.AiChat;
using Api.Features.AiChat.Tools;
using Xunit;

namespace Api.Tests;

public class SampleTest
{
    [Fact]
    public void ToolExecutionResult_Ok_ReturnsSuccessTrue()
    {
        var result = ToolExecutionResult.Ok("test content");

        Assert.True(result.Success);
        Assert.Equal("test content", result.Content);
    }

    [Fact]
    public void ToolExecutionResult_Fail_ReturnsSuccessFalse()
    {
        var result = ToolExecutionResult.Fail("error message");

        Assert.False(result.Success);
        Assert.Equal("error message", result.Content);
    }

    [Fact]
    public void StreamEvent_DefaultType_IsEmptyString()
    {
        var evt = new StreamEvent();

        Assert.Equal("", evt.Type);
        Assert.Null(evt.Text);
        Assert.Null(evt.ToolName);
    }
}
