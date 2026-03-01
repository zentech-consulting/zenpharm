import { apiFetch } from './client'

export interface DashboardSummary {
  totalClients: number
  totalBookings: number
  totalEmployees: number
  revenue: number
  totalProducts: number
  lowStockCount: number
  expiringCount: number
  dailyStats: DailyStat[]
}

export interface DailyStat {
  date: string
  bookingCount: number
  revenue: number
}

export const fetchDashboardSummary = (from?: string, to?: string) => {
  const params = new URLSearchParams()
  if (from) params.set('from', from)
  if (to) params.set('to', to)
  return apiFetch<DashboardSummary>(`/api/reports/dashboard?${params}`)
}
