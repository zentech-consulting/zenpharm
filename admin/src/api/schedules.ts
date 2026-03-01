import { apiFetch } from './client'

export interface Schedule {
  id: string
  employeeId: string
  employeeName: string
  date: string
  startTime: string
  endTime: string
  location?: string
  notes?: string
  createdAt: string
}

interface ScheduleList { items: Schedule[]; totalCount: number }

export const fetchSchedules = (startDate?: string, endDate?: string, employeeId?: string) => {
  const params = new URLSearchParams()
  if (startDate) params.set('startDate', startDate)
  if (endDate) params.set('endDate', endDate)
  if (employeeId) params.set('employeeId', employeeId)
  return apiFetch<ScheduleList>(`/api/schedules?${params}`)
}

export const createSchedule = (data: { employeeId: string; date: string; startTime: string; endTime: string; location?: string; notes?: string }) =>
  apiFetch<Schedule>('/api/schedules', { method: 'POST', body: JSON.stringify(data) })

export const updateSchedule = (id: string, data: { startTime: string; endTime: string; location?: string; notes?: string }) =>
  apiFetch<Schedule>(`/api/schedules/${id}`, { method: 'PUT', body: JSON.stringify(data) })

export const deleteSchedule = (id: string) =>
  apiFetch<void>(`/api/schedules/${id}`, { method: 'DELETE' })

export const generateSchedules = (data: { startDate: string; endDate: string; employeeIds?: string[] }) =>
  apiFetch<Schedule[]>('/api/schedules/generate', { method: 'POST', body: JSON.stringify(data) })
