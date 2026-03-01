import { apiFetch } from './client'

export interface MasterProduct {
  id: string
  sku: string
  name: string
  category: string
  description?: string
  unitPrice: number
  unit: string
  genericName?: string
  brand?: string
  barcode?: string
  scheduleClass: string
  packSize?: string
  activeIngredients?: string
  warnings?: string
  pbsItemCode?: string
  imageUrl?: string
  isActive: boolean
  createdAt: string
}

interface MasterProductList { items: MasterProduct[]; totalCount: number }

export const fetchMasterProducts = (page = 1, pageSize = 20, search?: string, category?: string, scheduleClass?: string) => {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (search) params.set('search', search)
  if (category) params.set('category', category)
  if (scheduleClass) params.set('scheduleClass', scheduleClass)
  return apiFetch<MasterProductList>(`/api/master-products?${params}`)
}
