import { useState, useEffect, useCallback } from 'react'
import { Search } from 'lucide-react'
import { fetchProducts, fetchCategories, type ShopProduct } from '../api/shop'
import ProductCard from '../components/ProductCard'

export default function ShopPage() {
  const [products, setProducts] = useState<ShopProduct[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [categories, setCategories] = useState<string[]>([])
  const [selectedCategory, setSelectedCategory] = useState<string>('')
  const [searchTerm, setSearchTerm] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const pageSize = 20

  // Debounce search
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchTerm), 300)
    return () => clearTimeout(timer)
  }, [searchTerm])

  // Reset page when filters change
  useEffect(() => {
    setPage(1)
  }, [selectedCategory, debouncedSearch])

  // Fetch categories
  useEffect(() => {
    fetchCategories()
      .then(setCategories)
      .catch(err => console.error('Failed to load categories:', err))
  }, [])

  // Fetch products
  const loadProducts = useCallback(async () => {
    setLoading(true)
    try {
      const result = await fetchProducts({
        category: selectedCategory || undefined,
        search: debouncedSearch || undefined,
        page,
        pageSize,
      })
      setProducts(result.items)
      setTotalCount(result.totalCount)
    } catch (err) {
      console.error('Failed to load products:', err)
    } finally {
      setLoading(false)
    }
  }, [selectedCategory, debouncedSearch, page])

  useEffect(() => {
    loadProducts()
  }, [loadProducts])

  const totalPages = Math.ceil(totalCount / pageSize)

  return (
    <div className="mx-auto max-w-7xl px-6 py-10">
      {/* Hero */}
      <div className="mb-10 text-center">
        <h1 className="text-3xl font-bold text-primary md:text-4xl">
          Browse Our Products
        </h1>
        <p className="mt-2 text-gray-500">
          Quality pharmacy products available for click-and-collect
        </p>
      </div>

      {/* Search */}
      <div className="relative mb-6">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" size={20} />
        <input
          type="text"
          placeholder="Search products..."
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          className="w-full rounded-lg border border-surface-dark bg-white py-3 pl-10 pr-4 text-sm outline-none transition-colors focus:border-highlight"
        />
      </div>

      {/* Category pills */}
      <div className="mb-8 flex flex-wrap gap-2">
        <button
          onClick={() => setSelectedCategory('')}
          className={`rounded-full px-4 py-1.5 text-sm font-medium transition-colors ${
            !selectedCategory
              ? 'bg-primary text-white'
              : 'bg-surface text-gray-600 hover:bg-surface-dark'
          }`}
        >
          All
        </button>
        {categories.map(cat => (
          <button
            key={cat}
            onClick={() => setSelectedCategory(cat)}
            className={`rounded-full px-4 py-1.5 text-sm font-medium transition-colors ${
              selectedCategory === cat
                ? 'bg-primary text-white'
                : 'bg-surface text-gray-600 hover:bg-surface-dark'
            }`}
          >
            {cat}
          </button>
        ))}
      </div>

      {/* Loading */}
      {loading && (
        <div className="flex justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
        </div>
      )}

      {/* Product grid */}
      {!loading && products.length === 0 && (
        <div className="py-20 text-center text-gray-400">
          No products found. Try a different search or category.
        </div>
      )}

      {!loading && products.length > 0 && (
        <>
          <div className="grid grid-cols-2 gap-4 sm:gap-6 md:grid-cols-3 lg:grid-cols-4">
            {products.map(p => (
              <ProductCard key={p.id} product={p} />
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="mt-10 flex items-center justify-center gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="rounded-lg border border-surface-dark px-4 py-2 text-sm font-medium disabled:opacity-40"
              >
                Previous
              </button>
              <span className="px-4 text-sm text-gray-500">
                Page {page} of {totalPages}
              </span>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="rounded-lg border border-surface-dark px-4 py-2 text-sm font-medium disabled:opacity-40"
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
    </div>
  )
}
