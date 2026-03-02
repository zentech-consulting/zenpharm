using Api.Common;
using Dapper;

namespace Api.Features.Notifications;

internal sealed class NotificationService(
    ITenantDb db,
    IConfiguration cfg,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<NotificationResult> SendBookingReminderAsync(Guid bookingId, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var booking = await conn.QuerySingleOrDefaultAsync<BookingReminderData>(
            new CommandDefinition("""
                SELECT b.Id, b.StartTime, b.EndTime, b.Status,
                       c.FirstName, c.LastName, c.Phone,
                       s.Name AS ServiceName
                FROM dbo.Bookings b
                INNER JOIN dbo.Clients c ON c.Id = b.ClientId
                INNER JOIN dbo.Services s ON s.Id = b.ServiceId
                WHERE b.Id = @BookingId
                """,
                new { BookingId = bookingId },
                cancellationToken: ct));

        if (booking is null)
            return NotificationResult.Fail("Booking not found");

        if (booking.Status is "cancelled" or "completed")
            return NotificationResult.Fail($"Cannot send reminder for {booking.Status} booking");

        if (string.IsNullOrWhiteSpace(booking.Phone))
            return NotificationResult.Fail("Client does not have a phone number on file");

        var message = $"Reminder: Your {booking.ServiceName} appointment is on " +
                      $"{booking.StartTime:dd/MM/yyyy} at {booking.StartTime:HH:mm}. " +
                      $"Please call to reschedule if needed.";

        Sms.SmsSendResult smsResult;
        try
        {
            smsResult = await Sms.SendAsync(cfg, logger, booking.Phone, message, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMS send threw for booking {BookingId}", bookingId);
            return NotificationResult.Fail("SMS service unavailable. Please try again later.");
        }

        if (!smsResult.Success)
        {
            if (smsResult.Skipped)
            {
                logger.LogInformation("SMS reminder skipped for booking {BookingId}: {Reason}", bookingId, smsResult.Error);
                return NotificationResult.Fail($"SMS skipped: {smsResult.Error}");
            }

            return NotificationResult.Fail(smsResult.Error ?? "SMS send failed");
        }

        logger.LogInformation("Booking reminder sent for {BookingId} to {ClientName}",
            bookingId, $"{booking.FirstName} {booking.LastName}");

        return NotificationResult.Ok();
    }

    public async Task<NotificationResult> SendPrescriptionReadyAsync(Guid clientId, string message, CancellationToken ct = default)
    {
        using var conn = await db.CreateAsync();

        var client = await conn.QuerySingleOrDefaultAsync<ClientContactData>(
            new CommandDefinition(
                "SELECT Id, FirstName, LastName, Phone FROM dbo.Clients WHERE Id = @ClientId",
                new { ClientId = clientId },
                cancellationToken: ct));

        if (client is null)
            return NotificationResult.Fail("Client not found");

        if (string.IsNullOrWhiteSpace(client.Phone))
            return NotificationResult.Fail("Client does not have a phone number on file");

        var smsMessage = string.IsNullOrWhiteSpace(message)
            ? $"Hi {client.FirstName}, your prescription is ready for collection. Please visit us at your convenience."
            : message;

        Sms.SmsSendResult smsResult;
        try
        {
            smsResult = await Sms.SendAsync(cfg, logger, client.Phone, smsMessage, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMS send threw for client {ClientId}", clientId);
            return NotificationResult.Fail("SMS service unavailable. Please try again later.");
        }

        if (!smsResult.Success)
        {
            if (smsResult.Skipped)
            {
                logger.LogInformation("Prescription ready SMS skipped for client {ClientId}: {Reason}", clientId, smsResult.Error);
                return NotificationResult.Fail($"SMS skipped: {smsResult.Error}");
            }

            return NotificationResult.Fail(smsResult.Error ?? "SMS send failed");
        }

        logger.LogInformation("Prescription ready SMS sent to {ClientName} ({ClientId})",
            $"{client.FirstName} {client.LastName}", clientId);

        return NotificationResult.Ok();
    }

    private sealed record BookingReminderData(
        Guid Id, DateTimeOffset StartTime, DateTimeOffset EndTime, string Status,
        string FirstName, string LastName, string? Phone, string ServiceName);

    private sealed record ClientContactData(
        Guid Id, string FirstName, string LastName, string? Phone);
}
