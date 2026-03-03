import { apiFetch } from './client'

export interface ShopProduct {
  id: string
  name: string
  genericName?: string
  brand?: string
  category: string
  scheduleClass: string
  price: number
  imageUrl?: string
  stockAvailability: string
  isFeatured: boolean
}

export interface ShopProductDetail extends ShopProduct {
  activeIngredients?: string
  warnings?: string
  description?: string
}

export interface ShopProductList {
  items: ShopProduct[]
  totalCount: number
}

interface FetchProductsParams {
  category?: string
  search?: string
  featured?: boolean
  page?: number
  pageSize?: number
}

export async function fetchProducts(params: FetchProductsParams = {}): Promise<ShopProductList> {
  const qs = new URLSearchParams()
  qs.set('page', String(params.page ?? 1))
  qs.set('pageSize', String(params.pageSize ?? 20))
  if (params.category) qs.set('category', params.category)
  if (params.search) qs.set('search', params.search)
  if (params.featured) qs.set('featured', 'true')

  const res = await apiFetch(`/api/shop/products?${qs}`)
  if (!res.ok) throw new Error(`Failed to fetch products: ${res.status}`)
  return res.json()
}

export async function fetchProduct(id: string): Promise<ShopProductDetail> {
  const res = await apiFetch(`/api/shop/products/${id}`)
  if (!res.ok) throw new Error(`Failed to fetch product: ${res.status}`)
  return res.json()
}

export async function fetchCategories(): Promise<string[]> {
  const res = await apiFetch(`/api/shop/categories`)
  if (!res.ok) throw new Error(`Failed to fetch categories: ${res.status}`)
  return res.json()
}
