import { apiFetch } from './client'

export interface Client {
  id: string
  firstName: string
  lastName: string
  email?: string
  phone?: string
  notes?: string
  dateOfBirth?: string
  allergies?: string
  medicationNotes?: string
  tags?: string
  createdAt: string
}

interface ClientList { items: Client[]; totalCount: number }

export const fetchClients = (page = 1, pageSize = 20, search?: string) =>
  apiFetch<ClientList>(`/api/clients?page=${page}&pageSize=${pageSize}${search ? `&search=${encodeURIComponent(search)}` : ''}`)

export const fetchClient = (id: string) => apiFetch<Client>(`/api/clients/${id}`)

export const createClient = (data: Omit<Client, 'id' | 'createdAt'>) =>
  apiFetch<Client>('/api/clients', { method: 'POST', body: JSON.stringify(data) })

export const updateClient = (id: string, data: Omit<Client, 'id' | 'createdAt'>) =>
  apiFetch<Client>(`/api/clients/${id}`, { method: 'PUT', body: JSON.stringify(data) })

export const deleteClient = (id: string) =>
  apiFetch<void>(`/api/clients/${id}`, { method: 'DELETE' })
