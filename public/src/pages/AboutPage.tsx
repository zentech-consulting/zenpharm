import { Heart, Target, Users } from 'lucide-react'

const values = [
  {
    icon: Target,
    title: 'Our Mission',
    description:
      'To empower businesses with intelligent, tailored management solutions that streamline operations and drive growth.',
  },
  {
    icon: Users,
    title: 'Our Team',
    description:
      'A passionate team of consultants, developers, and industry specialists dedicated to delivering excellence.',
  },
  {
    icon: Heart,
    title: 'Our Values',
    description:
      'Innovation, integrity, and client-first thinking guide everything we do. Your success is our success.',
  },
]

export default function AboutPage() {
  return (
    <>
      <section className="bg-primary px-6 py-20 text-white">
        <div className="mx-auto max-w-4xl text-center">
          <h1 className="mb-6 text-4xl font-bold">About Us</h1>
          <p className="text-lg leading-relaxed opacity-80">
            We build intelligent business management platforms tailored to your
            industry. From booking systems to AI-powered customer support, we
            help you work smarter.
          </p>
        </div>
      </section>

      <section className="px-6 py-20">
        <div className="mx-auto max-w-5xl">
          <h2 className="mb-4 text-3xl font-bold text-primary">Our Story</h2>
          <p className="mb-6 text-lg leading-relaxed text-gray-600">
            Founded with the belief that every business deserves enterprise-grade
            tools, we set out to create a platform that adapts to any industry.
            Whether you run a salon, a tutoring centre, a clinic, or a
            consultancy, our platform moulds itself to your workflow.
          </p>
          <p className="text-lg leading-relaxed text-gray-600">
            Today, we serve businesses across multiple verticals, each with a
            customised experience built on our robust core platform. Our AI
            consultant learns your business and helps your customers around the
            clock.
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
