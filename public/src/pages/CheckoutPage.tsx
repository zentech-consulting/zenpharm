import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useCart } from '../contexts/CartContext'
import { placeOrder } from '../api/orders'

const checkoutSchema = z.object({
  firstName: z.string().min(1, 'First name is required').max(100),
  lastName: z.string().min(1, 'Last name is required').max(100),
  email: z.string().email('Valid email is required').max(200),
  phone: z.string().min(8, 'Valid phone number is required').max(20),
  notes: z.string().max(2000).optional(),
})

type CheckoutForm = z.infer<typeof checkoutSchema>

const GST_RATE = 0.10

export default function CheckoutPage() {
  const navigate = useNavigate()
  const { items, subtotal, clearCart } = useCart()
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CheckoutForm>({
    resolver: zodResolver(checkoutSchema),
  })

  const gst = Math.round(subtotal * GST_RATE * 100) / 100
  const total = subtotal + gst

  if (items.length === 0) {
    return (
      <div className="mx-auto max-w-7xl px-6 py-20 text-center">
        <h1 className="mb-2 text-2xl font-bold text-primary">No items in cart</h1>
        <p className="mb-6 text-gray-500">Add some products before checking out.</p>
        <Link
          to="/shop"
          className="inline-block rounded-lg bg-highlight px-6 py-3 font-medium text-white"
        >
          Browse Products
        </Link>
      </div>
    )
  }

  const onSubmit = async (data: CheckoutForm) => {
    setSubmitting(true)
    setError(null)
    try {
      const order = await placeOrder({
        ...data,
        notes: data.notes || undefined,
        items: items.map(i => ({ productId: i.productId, quantity: i.quantity })),
      })
      clearCart()
      navigate(`/order-confirmation/${order.orderNumber}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to place order. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="mx-auto max-w-7xl px-6 py-10">
      <h1 className="mb-8 text-2xl font-bold text-primary md:text-3xl">Checkout</h1>

      <div className="grid gap-10 lg:grid-cols-3">
        {/* Form */}
        <form onSubmit={handleSubmit(onSubmit)} className="lg:col-span-2 space-y-6">
          <div className="rounded-xl border border-surface-dark bg-white p-6">
            <h2 className="mb-4 text-lg font-bold text-primary">Your Details</h2>

            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">
                  First Name *
                </label>
                <input
                  {...register('firstName')}
                  className="w-full rounded-lg border border-surface-dark px-3 py-2 text-sm outline-none focus:border-highlight"
                />
                {errors.firstName && (
                  <p className="mt-1 text-xs text-red-500">{errors.firstName.message}</p>
                )}
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">
                  Last Name *
                </label>
                <input
                  {...register('lastName')}
                  className="w-full rounded-lg border border-surface-dark px-3 py-2 text-sm outline-none focus:border-highlight"
                />
                {errors.lastName && (
                  <p className="mt-1 text-xs text-red-500">{errors.lastName.message}</p>
                )}
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">
                  Email *
                </label>
                <input
                  type="email"
                  {...register('email')}
                  className="w-full rounded-lg border border-surface-dark px-3 py-2 text-sm outline-none focus:border-highlight"
                />
                {errors.email && (
                  <p className="mt-1 text-xs text-red-500">{errors.email.message}</p>
                )}
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">
                  Phone *
                </label>
                <input
                  type="tel"
                  {...register('phone')}
                  className="w-full rounded-lg border border-surface-dark px-3 py-2 text-sm outline-none focus:border-highlight"
                />
                {errors.phone && (
                  <p className="mt-1 text-xs text-red-500">{errors.phone.message}</p>
                )}
              </div>
            </div>

            <div className="mt-4">
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Notes (optional)
              </label>
              <textarea
                {...register('notes')}
                rows={3}
                placeholder="Any special instructions for your order..."
                className="w-full rounded-lg border border-surface-dark px-3 py-2 text-sm outline-none focus:border-highlight"
              />
            </div>
          </div>

          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
              {error}
            </div>
          )}

          <button
            type="submit"
            disabled={submitting}
            className="w-full rounded-lg bg-highlight py-3 font-medium text-white transition-colors hover:bg-highlight/90 disabled:opacity-60"
          >
            {submitting ? (
              <span className="flex items-center justify-center gap-2">
                <span className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                Placing Order...
              </span>
            ) : (
              `Place Order — $${total.toFixed(2)}`
            )}
          </button>
        </form>

        {/* Order Summary */}
        <div className="lg:col-span-1">
          <div className="sticky top-6 rounded-xl border border-surface-dark bg-white p-6">
            <h2 className="mb-4 text-lg font-bold text-primary">Order Summary</h2>

            <div className="max-h-60 space-y-3 overflow-y-auto border-b border-surface-dark pb-4">
              {items.map(item => (
                <div key={item.productId} className="flex justify-between text-sm">
                  <span className="text-gray-600">
                    {item.name} <span className="text-gray-400">x{item.quantity}</span>
                  </span>
                  <span className="font-medium">${(item.price * item.quantity).toFixed(2)}</span>
                </div>
              ))}
            </div>

            <div className="space-y-2 border-b border-surface-dark py-4 text-sm">
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

            <p className="text-xs text-gray-400">
              Click-and-collect only. We&apos;ll SMS you when your order is ready.
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
