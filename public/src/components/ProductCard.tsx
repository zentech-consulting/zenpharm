import { ShoppingCart } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { ShopProduct } from '../api/shop'
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

export default function ProductCard({ product }: { product: ShopProduct }) {
  const { addItem } = useCart()
  const outOfStock = product.stockAvailability === 'Out of Stock'

  const handleAdd = (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (outOfStock) return
    addItem({
      productId: product.id,
      name: product.name,
      price: product.price,
      imageUrl: product.imageUrl,
      scheduleClass: product.scheduleClass,
      stockAvailability: product.stockAvailability,
    })
  }

  return (
    <Link
      to={`/shop/${product.id}`}
      className="group flex flex-col overflow-hidden rounded-xl border border-surface-dark bg-white transition-shadow hover:shadow-lg"
    >
      <div className="flex h-48 items-center justify-center bg-surface p-4">
        {product.imageUrl ? (
          <img
            src={product.imageUrl}
            alt={product.name}
            className="h-full w-full object-contain"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-4xl text-surface-dark">
            💊
          </div>
        )}
      </div>

      <div className="flex flex-1 flex-col p-4">
        <div className="mb-2 flex items-center gap-2">
          <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${scheduleColours[product.scheduleClass] ?? 'bg-gray-100 text-gray-700'}`}>
            {product.scheduleClass}
          </span>
          {product.isFeatured && (
            <span className="rounded-full bg-highlight/10 px-2 py-0.5 text-xs font-medium text-highlight">
              Featured
            </span>
          )}
        </div>

        <h3 className="mb-1 font-semibold text-primary group-hover:text-highlight transition-colors">
          {product.name}
        </h3>
        {product.brand && (
          <p className="mb-2 text-xs text-gray-500">{product.brand}</p>
        )}

        <div className="mt-auto flex items-end justify-between pt-3">
          <div>
            <p className="text-lg font-bold text-primary">
              ${product.price.toFixed(2)}
            </p>
            <p className={`text-xs font-medium ${stockColours[product.stockAvailability] ?? 'text-gray-500'}`}>
              {product.stockAvailability}
            </p>
          </div>

          <button
            onClick={handleAdd}
            disabled={outOfStock}
            className="flex items-center gap-1.5 rounded-lg bg-highlight px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-highlight/90 disabled:cursor-not-allowed disabled:opacity-40"
          >
            <ShoppingCart size={16} />
            Add
          </button>
        </div>
      </div>
    </Link>
  )
}
