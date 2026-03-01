import { Link } from 'react-router-dom'
import { ArrowRight, Sparkles, Calendar, Users, Package, ClipboardList, BarChart3 } from 'lucide-react'

const features = [
  {
    icon: Package,
    title: 'Shared Catalogue',
    description: 'Import from a centralised product catalogue with PBS codes, schedule classes, and pharmacy-specific data.',
  },
  {
    icon: ClipboardList,
    title: 'Inventory Management',
    description: 'Track stock levels, set reorder alerts, monitor expiry dates, and record all stock movements.',
  },
  {
    icon: Users,
    title: 'Client Records',
    description: 'Complete patient profiles with allergies, medication notes, and date of birth for better care.',
  },
  {
    icon: Calendar,
    title: 'Scheduling & Bookings',
    description: 'Manage pharmacist consultations, flu vaccinations, and health checks with online booking.',
  },
  {
    icon: Sparkles,
    title: 'AI Pharmacy Assistant',
    description: 'Intelligent AI that answers customer questions about medications, opening hours, and services.',
  },
  {
    icon: BarChart3,
    title: 'Reports & Insights',
    description: 'Dashboard with revenue, stock alerts, expiry tracking, and daily booking statistics.',
  },
]

export default function HomePage() {
  return (
    <>
      <section className="bg-primary px-6 py-24 text-white">
        <div className="mx-auto max-w-4xl text-center">
          <h1 className="mb-6 text-5xl font-bold leading-tight">
            Smart Pharmacy Management, <span className="text-highlight">Instantly</span>
          </h1>
          <p className="mb-8 text-lg opacity-80">
            The all-in-one platform built for Australian independent pharmacies.
            Catalogue, inventory, clients, scheduling, and AI — ready to go.
          </p>
          <div className="flex justify-center gap-4">
            <Link
              to="/pricing"
              className="inline-flex items-center gap-2 rounded-lg bg-highlight px-6 py-3 font-semibold text-white transition-opacity hover:opacity-90"
            >
              View Pricing <ArrowRight className="h-4 w-4" />
            </Link>
            <Link
              to="/contact"
              className="inline-flex items-center gap-2 rounded-lg border border-white/30 px-6 py-3 font-semibold text-white transition-colors hover:bg-white/10"
            >
              Book a Demo
            </Link>
          </div>
        </div>
      </section>

      <section className="px-6 py-20">
        <div className="mx-auto max-w-6xl">
          <h2 className="mb-12 text-center text-3xl font-bold text-primary">
            Everything Your Pharmacy Needs
          </h2>
          <div className="grid gap-8 md:grid-cols-2 lg:grid-cols-3">
            {features.map((feature) => (
              <div
                key={feature.title}
                className="rounded-xl border border-surface-dark p-8 transition-shadow hover:shadow-lg"
              >
                <feature.icon className="mb-4 h-10 w-10 text-highlight" />
                <h3 className="mb-2 text-xl font-semibold text-primary">{feature.title}</h3>
                <p className="text-sm leading-relaxed text-gray-600">{feature.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-surface px-6 py-20">
        <div className="mx-auto max-w-3xl text-center">
          <h2 className="mb-4 text-3xl font-bold text-primary">Ready to Modernise Your Pharmacy?</h2>
          <p className="mb-8 text-gray-600">
            Join independent pharmacies across Australia already using ZenPharm to
            streamline operations and deliver better patient care.
          </p>
          <Link
            to="/contact"
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-8 py-3 font-semibold text-white transition-opacity hover:opacity-90"
          >
            Get Started <ArrowRight className="h-4 w-4" />
          </Link>
        </div>
      </section>
    </>
  )
}
