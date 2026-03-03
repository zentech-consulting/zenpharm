import { apiFetch } from './client'

export interface OrderItem {
  id: string
  tenantProductId: string
  productName: string
  quantity: number
  unitPrice: number
  subtotal: number
}

export interface Order {
  id: string
  orderNumber: string
  clientId: string
  clientName: string
  status: string
  subtotal: number
  taxAmount: number
  total: number
  notes?: string
  estimatedReadyAt?: string
  readyNotifiedAt?: string
  collectedAt?: string
  cancelledAt?: string
  cancellationReason?: string
  createdAt: string
  items: OrderItem[]
}

export interface OrderSummary {
  id: string
  orderNumber: string
  clientName: string
  status: string
  itemCount: number
  total: number
  estimatedReadyAt?: string
  createdAt: string
}

interface OrderList {
  items: OrderSummary[]
  totalCount: number
}

export const fetchOrders = (page = 1, pageSize = 20, status?: string, search?: string) => {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (status) params.set('status', status)
  if (search) params.set('search', search)
  return apiFetch<OrderList>(`/api/orders?${params}`)
}

export const fetchOrder = (id: string) =>
  apiFetch<Order>(`/api/orders/${id}`)

export const markOrderReady = (id: string) =>
  apiFetch<Order>(`/api/orders/${id}/mark-ready`, { method: 'POST' })

export const markOrderCollected = (id: string) =>
  apiFetch<Order>(`/api/orders/${id}/mark-collected`, { method: 'POST' })

export const cancelOrder = (id: string, reason: string) =>
  apiFetch<Order>(`/api/orders/${id}/cancel`, {
    method: 'POST',
    body: JSON.stringify({ reason }),
  })
