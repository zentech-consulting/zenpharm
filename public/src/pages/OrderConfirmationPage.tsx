import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { CheckCircle } from 'lucide-react'
import { trackOrder, type Order } from '../api/orders'

export default function OrderConfirmationPage() {
  const { orderNumber } = useParams<{ orderNumber: string }>()
  const [order, setOrder] = useState<Order | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!orderNumber) return
    trackOrder(orderNumber)
      .then(setOrder)
      .catch(err => console.error('Failed to load order:', err))
      .finally(() => setLoading(false))
  }, [orderNumber])

  if (loading) {
    return (
      <div className="flex justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (!order) {
    return (
      <div className="py-20 text-center">
        <p className="text-lg text-gray-500">Order not found</p>
        <Link to="/shop" className="mt-4 inline-block text-highlight hover:underline">
          Back to Shop
        </Link>
      </div>
    )
  }

  const estimatedReady = order.estimatedReadyAt
    ? new Date(order.estimatedReadyAt).toLocaleString('en-AU', {
        weekday: 'short',
        day: 'numeric',
        month: 'short',
        hour: 'numeric',
        minute: '2-digit',
        hour12: true,
      })
    : null

  return (
    <div className="mx-auto max-w-2xl px-6 py-16 text-center">
      <CheckCircle size={64} className="mx-auto mb-6 text-green-500" />

      <h1 className="mb-2 text-3xl font-bold text-primary">Order Confirmed!</h1>
      <p className="mb-8 text-gray-500">
        Thank you for your order. We&apos;ll prepare it for collection.
      </p>

      <div className="mb-8 rounded-xl border border-surface-dark bg-white p-8">
        <p className="mb-1 text-sm text-gray-500">Order Number</p>
        <p className="mb-6 text-3xl font-bold text-primary">{order.orderNumber}</p>

        {estimatedReady && (
          <div className="mb-6">
            <p className="mb-1 text-sm text-gray-500">Estimated Collection Time</p>
            <p className="text-lg font-semibold text-primary">{estimatedReady}</p>
          </div>
        )}

        <div className="mb-6 rounded-lg bg-blue-50 p-4 text-sm text-blue-700">
          We&apos;ll send you an SMS when your order is ready for collection.
        </div>

        {/* Order Items */}
        <div className="border-t border-surface-dark pt-4">
          <h2 className="mb-3 text-left text-sm font-semibold uppercase tracking-wider text-gray-400">
            Items Ordered
          </h2>
          <div className="space-y-2">
            {order.items.map(item => (
              <div key={item.id} className="flex justify-between text-sm">
                <span className="text-gray-600">
                  {item.productName} <span className="text-gray-400">x{item.quantity}</span>
                </span>
                <span className="font-medium">${item.subtotal.toFixed(2)}</span>
              </div>
            ))}
          </div>

          <div className="mt-4 space-y-1 border-t border-surface-dark pt-3 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-500">Subtotal</span>
              <span>${order.subtotal.toFixed(2)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-500">GST</span>
              <span>${order.taxAmount.toFixed(2)}</span>
            </div>
            <div className="flex justify-between text-base font-bold">
              <span>Total</span>
              <span className="text-primary">${order.total.toFixed(2)}</span>
            </div>
          </div>
        </div>
      </div>

      <Link
        to="/shop"
        className="inline-block rounded-lg bg-highlight px-6 py-3 font-medium text-white transition-colors hover:bg-highlight/90"
      >
        Continue Shopping
      </Link>
    </div>
  )
}
