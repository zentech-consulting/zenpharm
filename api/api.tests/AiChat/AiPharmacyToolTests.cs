using Api.Features.AiChat.Tools;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Tests.AiChat;

public class AiPharmacyToolTests
{
    // ================================================================
    // ToolExecutionResult Tests
    // ================================================================

    [Fact]
    public void ToolExecutionResult_Ok_CreatesSuccessResult()
    {
        var result = ToolExecutionResult.Ok("Product found: Panadol 500mg");

        Assert.True(result.Success);
        Assert.Equal("Product found: Panadol 500mg", result.Content);
    }

    [Fact]
    public void ToolExecutionResult_Fail_CreatesFailureResult()
    {
        var result = ToolExecutionResult.Fail("Product not found");

        Assert.False(result.Success);
        Assert.Equal("Product not found", result.Content);
    }

    [Fact]
    public void ToolExecutionResult_RecordEquality()
    {
        var a = ToolExecutionResult.Ok("test content");
        var b = ToolExecutionResult.Ok("test content");

        Assert.Equal(a, b);
    }

    [Fact]
    public void ToolExecutionResult_Inequality_DifferentSuccess()
    {
        var ok = ToolExecutionResult.Ok("content");
        var fail = ToolExecutionResult.Fail("content");

        Assert.NotEqual(ok, fail);
    }

    // ================================================================
    // Tool Definition Tests
    // ================================================================

    [Fact]
    public void GetToolDefinitions_IncludesSearchProducts()
    {
        var executor = CreateExecutor();
        var tools = executor.GetToolDefinitions();

        Assert.Contains(tools, t =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(t);
            return json.Contains("search_products");
        });
    }

    [Fact]
    public void GetToolDefinitions_IncludesCheckStock()
    {
        var executor = CreateExecutor();
        var tools = executor.GetToolDefinitions();

        Assert.Contains(tools, t =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(t);
            return json.Contains("check_stock");
        });
    }

    [Fact]
    public void GetToolDefinitions_IncludesMedicationInfo()
    {
        var executor = CreateExecutor();
        var tools = executor.GetToolDefinitions();

        Assert.Contains(tools, t =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(t);
            return json.Contains("medication_info");
        });
    }

    [Fact]
    public void GetToolDefinitions_Has6Tools()
    {
        var executor = CreateExecutor();
        var tools = executor.GetToolDefinitions();

        // 3 original + 3 pharmacy tools
        Assert.Equal(6, tools.Count);
    }

    [Fact]
    public void GetToolDefinitions_AllToolsHaveRequiredSchema()
    {
        var executor = CreateExecutor();
        var tools = executor.GetToolDefinitions();

        foreach (var tool in tools)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(tool);
            Assert.Contains("name", json);
            Assert.Contains("description", json);
            Assert.Contains("input_schema", json);
        }
    }

    [Fact]
    public async Task ExecuteAsync_UnknownTool_ReturnsFail()
    {
        var executor = CreateExecutor();
        var input = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>("{}");

        var result = await executor.ExecuteAsync("nonexistent_tool", input);

        Assert.False(result.Success);
        Assert.Contains("Unknown tool", result.Content);
    }

    // ================================================================
    // Helper
    // ================================================================

    private static AiToolExecutor CreateExecutor()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var sp = services.BuildServiceProvider();
        return new AiToolExecutor(sp, Microsoft.Extensions.Logging.Abstractions.NullLogger<AiToolExecutor>.Instance);
    }
}
