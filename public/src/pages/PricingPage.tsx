import { Link } from 'react-router-dom'
import { Check } from 'lucide-react'

interface PlanProps {
  name: string
  price: string
  period: string
  description: string
  features: string[]
  highlighted?: boolean
  cta: string
  ctaLink: string
}

function PlanCard({ name, price, period, description, features, highlighted, cta, ctaLink }: PlanProps) {
  return (
    <div
      className={`flex flex-col rounded-2xl border p-8 ${
        highlighted
          ? 'border-highlight shadow-xl ring-2 ring-highlight'
          : 'border-surface-dark'
      }`}
    >
      <h3 className="mb-2 text-2xl font-bold text-primary">{name}</h3>
      <p className="mb-4 text-sm text-gray-500">{description}</p>
      <div className="mb-6">
        <span className="text-4xl font-bold text-primary">{price}</span>
        <span className="text-gray-500">{period}</span>
      </div>
      <ul className="mb-8 flex-1 space-y-3">
        {features.map((feature) => (
          <li key={feature} className="flex items-start gap-2 text-sm text-gray-600">
            <Check className="mt-0.5 h-4 w-4 flex-shrink-0 text-highlight" />
            {feature}
          </li>
        ))}
      </ul>
      <Link
        to={ctaLink}
        className={`block rounded-lg px-6 py-3 text-center font-semibold transition-opacity hover:opacity-90 ${
          highlighted ? 'bg-highlight text-white' : 'bg-primary text-white'
        }`}
      >
        {cta}
      </Link>
    </div>
  )
}

const plans: PlanProps[] = [
  {
    name: 'Basic',
    price: '$79',
    period: '/month',
    description: 'Everything you need to get started',
    features: [
      'Shared product catalogue',
      'Inventory management & stock alerts',
      'Client records with pharmacy fields',
      'Employee management & scheduling',
      'Booking system',
      'Dashboard & basic reports',
      'Up to 5 users',
      'Email support',
    ],
    cta: 'Get Started',
    ctaLink: '/signup?plan=Basic',
  },
  {
    name: 'Premium',
    price: '$199',
    period: '/month',
    description: 'Advanced features for growing pharmacies',
    highlighted: true,
    features: [
      'Everything in Basic',
      'AI Pharmacy Assistant',
      'Online store & click-and-collect',
      'Advanced analytics & reports',
      'SMS notifications',
      'Knowledge base management',
      'Up to 20 users',
      'Priority support',
    ],
    cta: 'Get Started',
    ctaLink: '/signup?plan=Premium',
  },
  {
    name: 'Enterprise',
    price: 'Custom',
    period: '',
    description: 'For multi-location pharmacy groups',
    features: [
      'Everything in Premium',
      'Multi-location management',
      'Custom integrations',
      'Dedicated account manager',
      'On-site training',
      'SLA guarantee',
      'Unlimited users',
      'Phone & email support',
    ],
    cta: 'Contact Us',
    ctaLink: '/contact',
  },
]

export default function PricingPage() {
  return (
    <>
      <section className="bg-primary px-6 py-20 text-white">
        <div className="mx-auto max-w-4xl text-center">
          <h1 className="mb-6 text-4xl font-bold">Simple, Transparent Pricing</h1>
          <p className="text-lg opacity-80">
            Choose the plan that fits your pharmacy. No hidden fees, no lock-in contracts.
          </p>
        </div>
      </section>

      <section className="px-6 py-20">
        <div className="mx-auto grid max-w-6xl gap-8 md:grid-cols-3">
          {plans.map((plan) => (
            <PlanCard key={plan.name} {...plan} />
          ))}
        </div>
      </section>

      <section className="bg-surface px-6 py-16">
        <div className="mx-auto max-w-3xl text-center">
          <h2 className="mb-4 text-2xl font-bold text-primary">Not sure which plan is right?</h2>
          <p className="mb-6 text-gray-600">
            Book a free demo and we&apos;ll walk you through everything. No pressure, no obligations.
          </p>
          <Link
            to="/contact"
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-8 py-3 font-semibold text-white transition-opacity hover:opacity-90"
          >
            Book a Demo
          </Link>
        </div>
      </section>
    </>
  )
}
