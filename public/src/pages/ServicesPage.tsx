import { useEffect, useState } from 'react'
import { Clock, DollarSign } from 'lucide-react'
import { fetchServices, type Service } from '../api/services'

export default function ServicesPage() {
  const [services, setServices] = useState<Service[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>()

  useEffect(() => {
    const load = async () => {
      try {
        const data = await fetchServices()
        setServices(data.items.filter(s => s.isActive))
      } catch {
        setError('Unable to load services. Please try again later.')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  const categories = [...new Set(services.map(s => s.category))].filter(Boolean)

  return (
    <section className="px-6 py-20">
      <div className="mx-auto max-w-6xl">
        <h1 className="mb-6 text-4xl font-bold text-primary">Our Services</h1>
        <p className="mb-12 text-lg text-gray-600">
          Discover the services we offer. Browse our catalogue and find the right
          service for you.
        </p>

        {loading && (
          <div className="flex justify-center py-20">
            <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
          </div>
        )}

        {error && (
          <div className="rounded-xl border border-highlight/30 bg-highlight/5 p-6 text-center text-highlight">
            {error}
          </div>
        )}

        {!loading && !error && services.length === 0 && (
          <div className="rounded-xl border border-surface-dark bg-surface p-12 text-center text-gray-500">
            No services available yet. Check back soon!
          </div>
        )}

        {!loading && !error && services.length > 0 && (
          <div className="space-y-12">
            {categories.map(cat => (
              <div key={cat}>
                <h2 className="mb-6 text-2xl font-semibold text-secondary">{cat}</h2>
                <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
                  {services.filter(s => s.category === cat).map(service => (
                    <div
                      key={service.id}
                      className="rounded-xl border border-surface-dark bg-white p-6 transition-shadow hover:shadow-lg"
                    >
                      <h3 className="mb-2 text-lg font-semibold text-primary">{service.name}</h3>
                      {service.description && (
                        <p className="mb-4 text-sm leading-relaxed text-gray-600">
                          {service.description}
                        </p>
                      )}
                      <div className="flex items-center gap-4 text-sm text-gray-500">
                        <span className="flex items-center gap-1">
                          <DollarSign className="h-4 w-4" />
                          {service.price.toFixed(2)}
                        </span>
                        <span className="flex items-center gap-1">
                          <Clock className="h-4 w-4" />
                          {service.durationMinutes} min
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </section>
  )
}
