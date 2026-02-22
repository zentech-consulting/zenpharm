import { Link } from 'react-router-dom'
import { ArrowRight, Sparkles, Calendar, Users } from 'lucide-react'

const features = [
  {
    icon: Calendar,
    title: 'Smart Booking',
    description: 'Online scheduling with real-time availability and automated reminders.',
  },
  {
    icon: Users,
    title: 'Client Management',
    description: 'Complete client profiles, history tracking, and communication tools.',
  },
  {
    icon: Sparkles,
    title: 'AI Consultant',
    description: 'Intelligent AI assistant to help your customers around the clock.',
  },
]

export default function HomePage() {
  return (
    <>
      <section className="bg-primary px-6 py-24 text-white">
        <div className="mx-auto max-w-4xl text-center">
          <h1 className="mb-6 text-5xl font-bold leading-tight">
            Your Industry, <span className="text-highlight">Managed Intelligently</span>
          </h1>
          <p className="mb-8 text-lg opacity-80">
            A complete business management platform tailored to your industry.
            Bookings, clients, staff, and AI — all in one place.
          </p>
          <div className="flex justify-center gap-4">
            <Link
              to="/contact"
              className="inline-flex items-center gap-2 rounded-lg bg-highlight px-6 py-3 font-semibold text-white transition-opacity hover:opacity-90"
            >
              Get Started <ArrowRight className="h-4 w-4" />
            </Link>
            <Link
              to="/services"
              className="inline-flex items-center gap-2 rounded-lg border border-white/30 px-6 py-3 font-semibold text-white transition-colors hover:bg-white/10"
            >
              Our Services
            </Link>
          </div>
        </div>
      </section>

      <section className="px-6 py-20">
        <div className="mx-auto max-w-6xl">
          <h2 className="mb-12 text-center text-3xl font-bold text-primary">
            Everything You Need
          </h2>
          <div className="grid gap-8 md:grid-cols-3">
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
          <h2 className="mb-4 text-3xl font-bold text-primary">Ready to Transform Your Business?</h2>
          <p className="mb-8 text-gray-600">
            Get in touch and we&apos;ll build a solution tailored to your industry.
          </p>
          <Link
            to="/contact"
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-8 py-3 font-semibold text-white transition-opacity hover:opacity-90"
          >
            Contact Us <ArrowRight className="h-4 w-4" />
          </Link>
        </div>
      </section>
    </>
  )
}
