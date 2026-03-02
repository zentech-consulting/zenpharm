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

export interface TopSellingProduct {
  productId: string
  productName: string
  brand?: string
  category: string
  totalSold: number
  totalRevenue: number
}

export interface TopSellingProductsReport {
  items: TopSellingProduct[]
  totalStockOutMovements: number
}

export interface RevenueByCategory {
  category: string
  bookingCount: number
  revenue: number
}

export interface RevenueByCategoryReport {
  items: RevenueByCategory[]
  totalRevenue: number
}

export interface ExpiryWaste {
  productId: string
  productName: string
  brand?: string
  expiredQuantity: number
  estimatedWasteValue: number
}

export interface ExpiryWasteReport {
  items: ExpiryWaste[]
  totalExpiredMovements: number
  totalWasteValue: number
}

export interface EmployeeUtilisation {
  employeeId: string
  employeeName: string
  role: string
  totalBookings: number
  completedBookings: number
  revenue: number
}

export interface EmployeeUtilisationReport {
  items: EmployeeUtilisation[]
  totalBookings: number
}

const buildDateParams = (from?: string, to?: string) => {
  const params = new URLSearchParams()
  if (from) params.set('from', from)
  if (to) params.set('to', to)
  return params
}

export const fetchDashboardSummary = (from?: string, to?: string) =>
  apiFetch<DashboardSummary>(`/api/reports/dashboard?${buildDateParams(from, to)}`)

export const fetchTopSellingProducts = (from?: string, to?: string, limit = 10) => {
  const params = buildDateParams(from, to)
  params.set('limit', String(limit))
  return apiFetch<TopSellingProductsReport>(`/api/reports/top-selling-products?${params}`)
}

export const fetchRevenueByCategory = (from?: string, to?: string) =>
  apiFetch<RevenueByCategoryReport>(`/api/reports/revenue-by-category?${buildDateParams(from, to)}`)

export const fetchExpiryWaste = (from?: string, to?: string) =>
  apiFetch<ExpiryWasteReport>(`/api/reports/expiry-waste?${buildDateParams(from, to)}`)

export const fetchEmployeeUtilisation = (from?: string, to?: string) =>
  apiFetch<EmployeeUtilisationReport>(`/api/reports/employee-utilisation?${buildDateParams(from, to)}`)
