import { apiFetch } from './client'

export interface NotificationResult {
  success: boolean
  error?: string
}

export const sendBookingReminder = (bookingId: string) =>
  apiFetch<NotificationResult>(`/api/notifications/booking-reminder/${bookingId}`, { method: 'POST' })

export const sendPrescriptionReady = (clientId: string, message: string) =>
  apiFetch<NotificationResult>('/api/notifications/prescription-ready', {
    method: 'POST',
    body: JSON.stringify({ clientId, message }),
  })
