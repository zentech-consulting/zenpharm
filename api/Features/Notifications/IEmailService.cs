namespace Api.Features.Notifications;

public interface IEmailService
{
    Task<EmailSendResult> SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public sealed record EmailSendResult(bool Success, string? Error = null)
{
    public static EmailSendResult Ok() => new(true);
    public static EmailSendResult Fail(string error) => new(false, error);
}

internal sealed class StubEmailService(
    ILogger<StubEmailService> logger) : IEmailService
{
    public Task<EmailSendResult> SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation("Email stub: To={To} Subject={Subject}", to, subject);
        throw new NotImplementedException("Email service not yet implemented — see Phase 1, Subtask 11");
    }
}
