import { apiFetch } from './client'

export interface Booking {
  id: string
  clientId: string
  clientName: string
  serviceId: string
  serviceName: string
  employeeId?: string
  employeeName?: string
  startTime: string
  endTime: string
  status: string
  notes?: string
  createdAt: string
}

interface BookingList { items: Booking[]; totalCount: number }

export const fetchBookings = (page = 1, pageSize = 20, date?: string, employeeId?: string) => {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (date) params.set('date', date)
  if (employeeId) params.set('employeeId', employeeId)
  return apiFetch<BookingList>(`/api/bookings?${params}`)
}

export const fetchBooking = (id: string) => apiFetch<Booking>(`/api/bookings/${id}`)

export const createBooking = (data: { clientId: string; serviceId: string; employeeId?: string; startTime: string; notes?: string }) =>
  apiFetch<Booking>('/api/bookings', { method: 'POST', body: JSON.stringify(data) })

export const updateBooking = (id: string, data: { employeeId?: string; startTime: string; status: string; notes?: string }) =>
  apiFetch<Booking>(`/api/bookings/${id}`, { method: 'PUT', body: JSON.stringify(data) })

export const cancelBooking = (id: string) =>
  apiFetch<void>(`/api/bookings/${id}`, { method: 'DELETE' })
