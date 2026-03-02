const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

export interface Service {
  id: string
  name: string
  description?: string
  category: string
  price: number
  durationMinutes: number
  isActive: boolean
}

interface ServiceList {
  items: Service[]
  totalCount: number
}

export async function fetchServices(category?: string): Promise<ServiceList> {
  const params = new URLSearchParams({ page: '1', pageSize: '50' })
  if (category) params.set('category', category)

  const res = await fetch(`${API_BASE}/api/services?${params}`)
  if (!res.ok) throw new Error(`Failed to fetch services: ${res.status}`)
  return res.json()
}
