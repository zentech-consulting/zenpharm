import { apiFetch } from './client'

export interface Employee {
  id: string
  firstName: string
  lastName: string
  email?: string
  phone?: string
  role: string
  isActive: boolean
  createdAt: string
}

interface EmployeeList { items: Employee[]; totalCount: number }

export const fetchEmployees = (page = 1, pageSize = 20, role?: string) =>
  apiFetch<EmployeeList>(`/api/employees?page=${page}&pageSize=${pageSize}${role ? `&role=${encodeURIComponent(role)}` : ''}`)

export const fetchEmployee = (id: string) => apiFetch<Employee>(`/api/employees/${id}`)

export const createEmployee = (data: Omit<Employee, 'id' | 'createdAt'>) =>
  apiFetch<Employee>('/api/employees', { method: 'POST', body: JSON.stringify(data) })

export const updateEmployee = (id: string, data: Omit<Employee, 'id' | 'createdAt'>) =>
  apiFetch<Employee>(`/api/employees/${id}`, { method: 'PUT', body: JSON.stringify(data) })

export const deleteEmployee = (id: string) =>
  apiFetch<void>(`/api/employees/${id}`, { method: 'DELETE' })
