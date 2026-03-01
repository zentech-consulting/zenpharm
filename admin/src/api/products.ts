import { apiFetch } from './client'

export interface Product {
  id: string
  masterProductId: string
  masterProductName: string
  genericName?: string
  brand?: string
  category: string
  scheduleClass: string
  defaultPrice: number
  customName?: string
  customPrice?: number
  imageUrl?: string
  stockQuantity: number
  reorderLevel: number
  expiryDate?: string
  isVisible: boolean
  isFeatured: boolean
  sortOrder: number
  createdAt: string
}

export interface StockMovement {
  id: string
  tenantProductId: string
  movementType: string
  quantity: number
  reference?: string
  notes?: string
  createdAt: string
  createdBy?: string
}

interface ProductList { items: Product[]; totalCount: number }
interface StockMovementList { items: StockMovement[]; totalCount: number }

export const fetchProducts = (page = 1, pageSize = 20, search?: string, lowStockOnly = false, expiringOnly = false) => {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (search) params.set('search', search)
  if (lowStockOnly) params.set('lowStockOnly', 'true')
  if (expiringOnly) params.set('expiringOnly', 'true')
  return apiFetch<ProductList>(`/api/products?${params}`)
}

export const fetchProduct = (id: string) =>
  apiFetch<Product>(`/api/products/${id}`)

export const importProducts = (masterProductIds: string[]) =>
  apiFetch<Product[]>('/api/products/import', { method: 'POST', body: JSON.stringify({ masterProductIds }) })

export const updateProduct = (id: string, data: {
  customName?: string
  customPrice?: number
  reorderLevel?: number
  expiryDate?: string
  isVisible?: boolean
  isFeatured?: boolean
  sortOrder?: number
}) =>
  apiFetch<Product>(`/api/products/${id}`, { method: 'PUT', body: JSON.stringify(data) })

export const deleteProduct = (id: string) =>
  apiFetch<void>(`/api/products/${id}`, { method: 'DELETE' })

export const recordStockMovement = (productId: string, data: {
  movementType: string
  quantity: number
  reference?: string
  notes?: string
  createdBy?: string
}) =>
  apiFetch<StockMovement>(`/api/products/${productId}/stock-movements`, { method: 'POST', body: JSON.stringify(data) })

export const fetchStockMovements = (productId: string, page = 1, pageSize = 20) =>
  apiFetch<StockMovementList>(`/api/products/${productId}/stock-movements?page=${page}&pageSize=${pageSize}`)
