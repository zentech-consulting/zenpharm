namespace Api.Features.Notifications;

public sealed record SendReminderRequest
{
    public Guid BookingId { get; init; }
}

public sealed record PrescriptionReadyRequest
{
    public Guid ClientId { get; init; }
    public string Message { get; init; } = "";
}

public sealed record NotificationResult(bool Success, string? Error = null)
{
    public static NotificationResult Ok() => new(true);
    public static NotificationResult Fail(string error) => new(false, error);
}
