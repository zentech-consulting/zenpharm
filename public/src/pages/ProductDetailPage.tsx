import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { ShoppingCart, ChevronRight, AlertTriangle } from 'lucide-react'
import { fetchProduct, type ShopProductDetail } from '../api/shop'
import { useCart } from '../contexts/CartContext'

const scheduleColours: Record<string, string> = {
  Unscheduled: 'bg-gray-100 text-gray-700',
  S2: 'bg-blue-100 text-blue-700',
  S3: 'bg-amber-100 text-amber-700',
}

const stockColours: Record<string, string> = {
  'In Stock': 'text-green-600',
  'Low Stock': 'text-amber-600',
  'Out of Stock': 'text-red-500',
}

export default function ProductDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { addItem } = useCart()
  const [product, setProduct] = useState<ShopProductDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [quantity, setQuantity] = useState(1)
  const [added, setAdded] = useState(false)

  useEffect(() => {
    if (!id) return
    setLoading(true)
    fetchProduct(id)
      .then(setProduct)
      .catch(err => console.error('Failed to load product:', err))
      .finally(() => setLoading(false))
  }, [id])

  const handleAdd = () => {
    if (!product) return
    addItem({
      productId: product.id,
      name: product.name,
      price: product.price,
      imageUrl: product.imageUrl,
      scheduleClass: product.scheduleClass,
      stockAvailability: product.stockAvailability,
    }, quantity)
    setAdded(true)
    setTimeout(() => setAdded(false), 2000)
  }

  if (loading) {
    return (
      <div className="flex justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (!product) {
    return (
      <div className="py-20 text-center">
        <p className="text-lg text-gray-500">Product not found</p>
        <Link to="/shop" className="mt-4 inline-block text-highlight hover:underline">
          Back to Shop
        </Link>
      </div>
    )
  }

  const outOfStock = product.stockAvailability === 'Out of Stock'

  return (
    <div className="mx-auto max-w-7xl px-6 py-10">
      {/* Breadcrumb */}
      <nav className="mb-8 flex items-center gap-2 text-sm text-gray-500">
        <Link to="/shop" className="hover:text-highlight">Shop</Link>
        <ChevronRight size={14} />
        <span className="text-gray-400">{product.category}</span>
        <ChevronRight size={14} />
        <span className="text-primary font-medium">{product.name}</span>
      </nav>

      <div className="grid gap-10 md:grid-cols-2">
        {/* Image */}
        <div className="flex items-center justify-center rounded-xl bg-surface p-8">
          {product.imageUrl ? (
            <img
              src={product.imageUrl}
              alt={product.name}
              className="max-h-80 w-full object-contain"
            />
          ) : (
            <div className="text-8xl">💊</div>
          )}
        </div>

        {/* Details */}
        <div>
          <div className="mb-3 flex items-center gap-2">
            <span className={`rounded-full px-3 py-1 text-sm font-medium ${scheduleColours[product.scheduleClass] ?? 'bg-gray-100 text-gray-700'}`}>
              {product.scheduleClass}
            </span>
            {product.isFeatured && (
              <span className="rounded-full bg-highlight/10 px-3 py-1 text-sm font-medium text-highlight">
                Featured
              </span>
            )}
          </div>

          <h1 className="mb-1 text-2xl font-bold text-primary md:text-3xl">
            {product.name}
          </h1>
          {product.brand && (
            <p className="mb-1 text-gray-500">{product.brand}</p>
          )}
          {product.genericName && (
            <p className="mb-4 text-sm text-gray-400">{product.genericName}</p>
          )}

          <p className="mb-2 text-3xl font-bold text-primary">
            ${product.price.toFixed(2)}
          </p>

          <p className={`mb-6 text-sm font-medium ${stockColours[product.stockAvailability] ?? 'text-gray-500'}`}>
            {product.stockAvailability}
          </p>

          {/* Quantity + Add to Cart */}
          <div className="mb-8 flex items-center gap-4">
            <div className="flex items-center rounded-lg border border-surface-dark">
              <button
                onClick={() => setQuantity(q => Math.max(1, q - 1))}
                className="px-3 py-2 text-lg font-medium hover:bg-surface"
              >
                −
              </button>
              <span className="min-w-[3rem] text-center font-medium">{quantity}</span>
              <button
                onClick={() => setQuantity(q => q + 1)}
                className="px-3 py-2 text-lg font-medium hover:bg-surface"
              >
                +
              </button>
            </div>

            <button
              onClick={handleAdd}
              disabled={outOfStock}
              className="flex items-center gap-2 rounded-lg bg-highlight px-6 py-3 font-medium text-white transition-colors hover:bg-highlight/90 disabled:cursor-not-allowed disabled:opacity-40"
            >
              <ShoppingCart size={18} />
              {added ? 'Added!' : 'Add to Cart'}
            </button>
          </div>

          {/* Description */}
          {product.description && (
            <div className="mb-6">
              <h2 className="mb-2 text-sm font-semibold uppercase tracking-wider text-gray-400">
                Description
              </h2>
              <p className="text-gray-600">{product.description}</p>
            </div>
          )}

          {/* Active Ingredients */}
          {product.activeIngredients && (
            <div className="mb-6">
              <h2 className="mb-2 text-sm font-semibold uppercase tracking-wider text-gray-400">
                Active Ingredients
              </h2>
              <p className="text-gray-600">{product.activeIngredients}</p>
            </div>
          )}

          {/* Warnings */}
          {product.warnings && (
            <div className="rounded-lg border border-amber-200 bg-amber-50 p-4">
              <div className="mb-1 flex items-center gap-2 text-amber-700">
                <AlertTriangle size={16} />
                <h2 className="text-sm font-semibold uppercase tracking-wider">
                  Warnings
                </h2>
              </div>
              <p className="text-sm text-amber-800">{product.warnings}</p>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
