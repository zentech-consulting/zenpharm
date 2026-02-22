import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { Send } from 'lucide-react'
import { useState } from 'react'

const contactSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100),
  email: z.string().email('Please enter a valid email address'),
  phone: z.string().max(20).optional(),
  message: z.string().min(10, 'Message must be at least 10 characters').max(2000),
})

type ContactFormData = z.infer<typeof contactSchema>

export default function ContactPage() {
  const [submitted, setSubmitted] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ContactFormData>()

  const onSubmit = (data: ContactFormData) => {
    // Contact form submission will be implemented with the notification module
    console.info('Contact form submitted:', data.name)
    setSubmitted(true)
  }

  if (submitted) {
    return (
      <section className="px-6 py-20">
        <div className="mx-auto max-w-2xl text-center">
          <h1 className="mb-4 text-4xl font-bold text-primary">Thank You!</h1>
          <p className="text-lg text-gray-600">
            We&apos;ve received your message and will be in touch shortly.
          </p>
        </div>
      </section>
    )
  }

  return (
    <section className="px-6 py-20">
      <div className="mx-auto max-w-2xl">
        <h1 className="mb-6 text-4xl font-bold text-primary">Contact Us</h1>
        <p className="mb-8 text-gray-600">
          Have a question or want to learn more? Fill in the form below and we&apos;ll
          get back to you.
        </p>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div>
            <label htmlFor="name" className="mb-1 block text-sm font-medium text-primary">
              Name *
            </label>
            <input
              id="name"
              type="text"
              {...register('name', { required: 'Name is required' })}
              className="w-full rounded-lg border border-surface-dark px-4 py-3 text-sm transition-colors focus:border-accent focus:outline-none"
              placeholder="Your name"
            />
            {errors.name && (
              <p className="mt-1 text-sm text-highlight">{errors.name.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="email" className="mb-1 block text-sm font-medium text-primary">
              Email *
            </label>
            <input
              id="email"
              type="email"
              {...register('email', { required: 'Email is required' })}
              className="w-full rounded-lg border border-surface-dark px-4 py-3 text-sm transition-colors focus:border-accent focus:outline-none"
              placeholder="you@example.com"
            />
            {errors.email && (
              <p className="mt-1 text-sm text-highlight">{errors.email.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="phone" className="mb-1 block text-sm font-medium text-primary">
              Phone
            </label>
            <input
              id="phone"
              type="tel"
              {...register('phone')}
              className="w-full rounded-lg border border-surface-dark px-4 py-3 text-sm transition-colors focus:border-accent focus:outline-none"
              placeholder="Optional"
            />
          </div>

          <div>
            <label htmlFor="message" className="mb-1 block text-sm font-medium text-primary">
              Message *
            </label>
            <textarea
              id="message"
              rows={5}
              {...register('message', { required: 'Message is required', minLength: { value: 10, message: 'At least 10 characters' } })}
              className="w-full rounded-lg border border-surface-dark px-4 py-3 text-sm transition-colors focus:border-accent focus:outline-none"
              placeholder="How can we help you?"
            />
            {errors.message && (
              <p className="mt-1 text-sm text-highlight">{errors.message.message}</p>
            )}
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-6 py-3 font-semibold text-white transition-opacity hover:opacity-90 disabled:opacity-50"
          >
            <Send className="h-4 w-4" />
            Send Message
          </button>
        </form>
      </div>
    </section>
  )
}
