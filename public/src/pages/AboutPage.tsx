import { Heart, Target, Shield } from 'lucide-react'

const values = [
  {
    icon: Target,
    title: 'Our Mission',
    description:
      'To empower Australian independent pharmacies with modern, affordable technology that simplifies daily operations and improves patient outcomes.',
  },
  {
    icon: Shield,
    title: 'Built for Pharmacy',
    description:
      'Designed from the ground up with PBS codes, schedule classes, stock tracking, and patient records — not a generic tool bolted onto pharmacy.',
  },
  {
    icon: Heart,
    title: 'Community First',
    description:
      'We believe independent pharmacies are the backbone of community healthcare. Our platform helps you compete with the chains while staying local.',
  },
]

export default function AboutPage() {
  return (
    <>
      <section className="bg-primary px-6 py-20 text-white">
        <div className="mx-auto max-w-4xl text-center">
          <h1 className="mb-6 text-4xl font-bold">About ZenPharm</h1>
          <p className="text-lg leading-relaxed opacity-80">
            We build smart pharmacy management software for Australian independent
            pharmacies — helping you manage inventory, serve patients better, and
            grow your business.
          </p>
        </div>
      </section>

      <section className="px-6 py-20">
        <div className="mx-auto max-w-5xl">
          <h2 className="mb-4 text-3xl font-bold text-primary">Our Story</h2>
          <p className="mb-6 text-lg leading-relaxed text-gray-600">
            ZenPharm was born from a simple observation: independent pharmacies
            across Australia are running on outdated systems or expensive enterprise
            software that doesn&apos;t fit their needs. We set out to change that.
          </p>
          <p className="text-lg leading-relaxed text-gray-600">
            Our platform brings together a shared product catalogue, real-time
            inventory management, patient records, scheduling, and AI-powered
            customer support — all in one affordable, cloud-based solution designed
            specifically for community pharmacies.
          </p>
        </div>
      </section>

      <section className="bg-surface px-6 py-20">
        <div className="mx-auto max-w-6xl">
          <div className="grid gap-8 md:grid-cols-3">
            {values.map((v) => (
              <div
                key={v.title}
                className="rounded-xl bg-white p-8 shadow-sm transition-shadow hover:shadow-md"
              >
                <v.icon className="mb-4 h-10 w-10 text-highlight" />
                <h3 className="mb-2 text-xl font-semibold text-primary">
                  {v.title}
                </h3>
                <p className="text-sm leading-relaxed text-gray-600">
                  {v.description}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>
    </>
  )
}
