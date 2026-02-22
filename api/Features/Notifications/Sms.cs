namespace Api.Features.Notifications;

public static class Sms
{
    public readonly record struct SmsSendResult(bool Success, string? Error = null, bool Skipped = false)
    {
        public static SmsSendResult Ok() => new(true);
        public static SmsSendResult Skip(string reason) => new(false, reason, true);
        public static SmsSendResult Fail(string error) => new(false, error, false);
    }

    public static async Task<SmsSendResult> SendAsync(
        IConfiguration cfg,
        ILogger logger,
        string phoneNumber,
        string message,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return SmsSendResult.Fail("Phone number is required");

        if (string.IsNullOrWhiteSpace(message))
            return SmsSendResult.Fail("Message is required");

        var enabled = cfg.GetValue<bool>("SmsBroadcast:Enabled");
        if (!enabled)
            return SmsSendResult.Skip("SMS is disabled in configuration");

        var dryRun = cfg.GetValue<bool>("SmsBroadcast:DryRun");
        if (dryRun)
        {
            logger.LogInformation("SMS DryRun: To={Phone} Message={Message}", phoneNumber, message);
            return SmsSendResult.Ok();
        }

        var username = cfg["SmsBroadcast:Username"];
        var password = cfg["SmsBroadcast:Password"];

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return SmsSendResult.Skip("SMS credentials not configured");

        try
        {
            // SMS provider integration will be implemented in Phase 1, Subtask 10
            await Task.CompletedTask;
            throw new NotImplementedException("SMS provider integration not yet implemented — see Phase 1, Subtask 10");
        }
        catch (NotImplementedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMS send failed. Phone={Phone}", phoneNumber);
            return SmsSendResult.Fail($"SMS send failed: {ex.Message}");
        }
    }
}
