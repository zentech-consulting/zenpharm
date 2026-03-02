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
        CancellationToken ct = default,
        HttpClient? externalClient = null)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return SmsSendResult.Fail("Phone number is required");

        if (string.IsNullOrWhiteSpace(message))
            return SmsSendResult.Fail("Message is required");

        var enabled = cfg.GetValue<bool>("SmsBroadcast:Enabled");
        if (!enabled)
            return SmsSendResult.Skip("SMS is disabled in configuration");

        var normalised = NormalisePhone(phoneNumber);
        var maskedPhone = MaskPhone(normalised);

        var dryRun = cfg.GetValue<bool>("SmsBroadcast:DryRun");
        if (dryRun)
        {
            logger.LogInformation("SMS DryRun: To={Phone} Msg={Message}", maskedPhone, message);
            return SmsSendResult.Ok();
        }

        var username = cfg["SmsBroadcast:Username"];
        var password = cfg["SmsBroadcast:Password"];
        var from = cfg["SmsBroadcast:From"] ?? "Zentech";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return SmsSendResult.Skip("SMS credentials not configured");

        try
        {
            var ownsClient = externalClient is null;
            var httpClient = externalClient ?? new HttpClient();
            try
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["username"] = username,
                    ["password"] = password,
                    ["to"] = normalised,
                    ["from"] = from,
                    ["message"] = message
                });

                var response = await httpClient.PostAsync(
                    "https://api.smsbroadcast.com.au/api-adv.php",
                    content, ct);

                var body = await response.Content.ReadAsStringAsync(ct);

                if (body.StartsWith("OK:", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("SMS sent successfully to {Phone}", maskedPhone);
                    return SmsSendResult.Ok();
                }

                logger.LogWarning("SMS provider returned: {Response} for {Phone}", body, maskedPhone);
                return SmsSendResult.Fail($"SMS provider error: {body}");
            }
            finally
            {
                if (ownsClient) httpClient.Dispose();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMS send failed. Phone={Phone}", maskedPhone);
            return SmsSendResult.Fail("SMS send failed. Check server logs for details.");
        }
    }

    internal static string NormalisePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        // Australian mobile: 04xx → 614xx
        if (digits.StartsWith("04") && digits.Length == 10)
            return "61" + digits[1..];

        // Already international
        if (digits.StartsWith("61") && digits.Length == 11)
            return digits;

        return digits;
    }

    internal static string MaskPhone(string phone)
    {
        if (phone.Length <= 4) return "****";
        return new string('*', phone.Length - 4) + phone[^4..];
    }
}
