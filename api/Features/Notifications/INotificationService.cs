namespace Api.Features.Notifications;

public interface INotificationService
{
    Task<NotificationResult> SendBookingReminderAsync(Guid bookingId, CancellationToken ct = default);
    Task<NotificationResult> SendPrescriptionReadyAsync(Guid clientId, string message, CancellationToken ct = default);
}
