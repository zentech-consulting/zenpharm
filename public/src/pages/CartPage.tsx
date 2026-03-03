import { Link } from 'react-router-dom'
import { Trash2, ShoppingBag } from 'lucide-react'
import { useCart } from '../contexts/CartContext'

const GST_RATE = 0.10

export default function CartPage() {
  const { items, subtotal, updateQuantity, removeItem } = useCart()

  const gst = Math.round(subtotal * GST_RATE * 100) / 100
  const total = subtotal + gst

  if (items.length === 0) {
    return (
      <div className="mx-auto max-w-7xl px-6 py-20 text-center">
        <ShoppingBag size={64} className="mx-auto mb-4 text-surface-dark" />
        <h1 className="mb-2 text-2xl font-bold text-primary">Your cart is empty</h1>
        <p className="mb-6 text-gray-500">Browse our products and add items to your cart.</p>
        <Link
          to="/shop"
          className="inline-block rounded-lg bg-highlight px-6 py-3 font-medium text-white transition-colors hover:bg-highlight/90"
        >
          Start Shopping
        </Link>
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-7xl px-6 py-10">
      <h1 className="mb-8 text-2xl font-bold text-primary md:text-3xl">Your Cart</h1>

      <div className="grid gap-10 lg:grid-cols-3">
        {/* Cart Items */}
        <div className="lg:col-span-2">
          <div className="divide-y divide-surface-dark rounded-xl border border-surface-dark bg-white">
            {items.map(item => (
              <div key={item.productId} className="flex items-center gap-4 p-4">
                <div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-lg bg-surface text-2xl">
                  {item.imageUrl ? (
                    <img src={item.imageUrl} alt={item.name} className="h-full w-full rounded-lg object-contain" />
                  ) : (
                    '💊'
                  )}
                </div>

                <div className="flex-1">
                  <Link to={`/shop/${item.productId}`} className="font-semibold text-primary hover:text-highlight">
                    {item.name}
                  </Link>
                  <p className="text-sm text-gray-500">${item.price.toFixed(2)} each</p>
                </div>

                <div className="flex items-center rounded-lg border border-surface-dark">
                  <button
                    onClick={() => updateQuantity(item.productId, item.quantity - 1)}
                    className="px-2.5 py-1 text-sm font-medium hover:bg-surface"
                  >
                    −
                  </button>
                  <span className="min-w-[2.5rem] text-center text-sm font-medium">{item.quantity}</span>
                  <button
                    onClick={() => updateQuantity(item.productId, item.quantity + 1)}
                    className="px-2.5 py-1 text-sm font-medium hover:bg-surface"
                  >
                    +
                  </button>
                </div>

                <p className="w-20 text-right font-semibold text-primary">
                  ${(item.price * item.quantity).toFixed(2)}
                </p>

                <button
                  onClick={() => removeItem(item.productId)}
                  className="text-gray-400 transition-colors hover:text-red-500"
                >
                  <Trash2 size={18} />
                </button>
              </div>
            ))}
          </div>

          <div className="mt-4">
            <Link to="/shop" className="text-sm text-highlight hover:underline">
              ← Continue Shopping
            </Link>
          </div>
        </div>

        {/* Order Summary */}
        <div className="lg:col-span-1">
          <div className="sticky top-6 rounded-xl border border-surface-dark bg-white p-6">
            <h2 className="mb-4 text-lg font-bold text-primary">Order Summary</h2>

            <div className="space-y-2 border-b border-surface-dark pb-4 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-500">Subtotal</span>
                <span className="font-medium">${subtotal.toFixed(2)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">GST (10%)</span>
                <span className="font-medium">${gst.toFixed(2)}</span>
              </div>
            </div>

            <div className="flex justify-between py-4 text-lg font-bold">
              <span>Total</span>
              <span className="text-primary">${total.toFixed(2)}</span>
            </div>

            <Link
              to="/checkout"
              className="block w-full rounded-lg bg-highlight py-3 text-center font-medium text-white transition-colors hover:bg-highlight/90"
            >
              Proceed to Checkout
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}
