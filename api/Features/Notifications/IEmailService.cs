using System.Net;
using System.Net.Mail;

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

internal sealed class DryRunEmailService(
    ILogger<DryRunEmailService> logger) : IEmailService
{
    public Task<EmailSendResult> SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation("Email DryRun: To={To} Subject={Subject} BodyLength={Length}",
            to, subject, htmlBody.Length);
        return Task.FromResult(EmailSendResult.Ok());
    }
}

internal sealed class SmtpEmailService(
    IConfiguration cfg,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task<EmailSendResult> SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = cfg["Email:SmtpHost"];
        var port = cfg.GetValue("Email:SmtpPort", 587);
        var fromAddress = cfg["Email:FromAddress"] ?? "noreply@zentech.com";
        var fromName = cfg["Email:FromName"] ?? "Zentech";
        var username = cfg["Email:Username"];
        var password = cfg["Email:Password"];

        if (string.IsNullOrEmpty(host))
            return EmailSendResult.Fail("SMTP host not configured");

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = !string.IsNullOrEmpty(username)
                    ? new NetworkCredential(username, password)
                    : null
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            await client.SendMailAsync(message, ct);

            logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            return EmailSendResult.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email send failed to {To}", to);
            return EmailSendResult.Fail("Email send failed. Check server logs for details.");
        }
    }
}
