const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

export interface OrderItemRequest {
  productId: string
  quantity: number
}

export interface PlaceOrderRequest {
  firstName: string
  lastName: string
  email: string
  phone: string
  notes?: string
  items: OrderItemRequest[]
}

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

export async function placeOrder(req: PlaceOrderRequest): Promise<Order> {
  const res = await fetch(`${API_BASE}/api/shop/orders`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  })
  if (!res.ok) {
    const data = await res.json().catch(() => null)
    throw new Error(data?.error ?? `Failed to place order: ${res.status}`)
  }
  return res.json()
}

export async function trackOrder(orderNumber: string): Promise<Order> {
  const res = await fetch(`${API_BASE}/api/shop/orders/${encodeURIComponent(orderNumber)}`)
  if (!res.ok) throw new Error(`Order not found: ${res.status}`)
  return res.json()
}
