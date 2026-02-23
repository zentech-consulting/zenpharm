using System.Runtime.CompilerServices;

namespace Api.Features.AiChat;

internal sealed class AiChatManager(
    IConfiguration cfg,
    ILogger<AiChatManager> logger) : IAiChatManager
{
    public Task<AiChatResponse> SendAsync(AiChatRequest request, string? clientIp, CancellationToken ct = default)
    {
        var dryRun = cfg.GetValue<bool>("AiChat:DryRun");
        if (dryRun)
        {
            logger.LogInformation("AiChat DryRun mode. MessageLength={Length}", request.Message?.Length ?? 0);
            return Task.FromResult(new AiChatResponse(
                Reply: "This is a dry-run response. AI chat is not yet connected.",
                Model: "dry-run",
                SessionToken: Guid.NewGuid().ToString("N")[..12],
                ToolResults: null));
        }

        throw new NotImplementedException("AiChat module not yet implemented — see Phase 1, Subtask 8");
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

        throw new NotImplementedException("AiChat streaming not yet implemented — see Phase 1, Subtask 8");
    }
}
