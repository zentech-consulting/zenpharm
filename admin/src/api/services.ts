import { apiFetch } from './client'

export interface Service {
  id: string
  name: string
  description?: string
  category: string
  price: number
  durationMinutes: number
  isActive: boolean
  createdAt: string
}

interface ServiceList { items: Service[]; totalCount: number }

export const fetchServices = (page = 1, pageSize = 20, category?: string) =>
  apiFetch<ServiceList>(`/api/services?page=${page}&pageSize=${pageSize}${category ? `&category=${encodeURIComponent(category)}` : ''}`)

export const fetchService = (id: string) => apiFetch<Service>(`/api/services/${id}`)

export const createService = (data: Omit<Service, 'id' | 'createdAt'>) =>
  apiFetch<Service>('/api/services', { method: 'POST', body: JSON.stringify(data) })

export const updateService = (id: string, data: Omit<Service, 'id' | 'createdAt'>) =>
  apiFetch<Service>(`/api/services/${id}`, { method: 'PUT', body: JSON.stringify(data) })

export const deleteService = (id: string) =>
  apiFetch<void>(`/api/services/${id}`, { method: 'DELETE' })
