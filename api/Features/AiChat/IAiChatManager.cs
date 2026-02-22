namespace Api.Features.AiChat;

public interface IAiChatManager
{
    Task<AiChatResponse> SendAsync(AiChatRequest request, string? clientIp, CancellationToken ct = default);
    IAsyncEnumerable<StreamEvent> SendStreamAsync(AiChatRequest request, string? clientIp, CancellationToken ct = default);
}
