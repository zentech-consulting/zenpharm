import { useState, useEffect, useCallback } from 'react'
import { useSearchParams, Link } from 'react-router-dom'
import { ArrowLeft, Check, X, Loader2 } from 'lucide-react'
import { getPlans, checkSubdomain, createCheckout } from '../api/signup'
import type { PlanSummary } from '../api/signup'

export default function SignupPage() {
  const [searchParams] = useSearchParams()
  const preselectedPlan = searchParams.get('plan') ?? 'Basic'

  const [plans, setPlans] = useState<PlanSummary[]>([])
  const [selectedPlanId, setSelectedPlanId] = useState<string>('')
  const [billingPeriod, setBillingPeriod] = useState<'Monthly' | 'Yearly'>('Monthly')

  const [pharmacyName, setPharmacyName] = useState('')
  const [subdomain, setSubdomain] = useState('')
  const [adminEmail, setAdminEmail] = useState('')
  const [adminFullName, setAdminFullName] = useState('')

  const [subdomainStatus, setSubdomainStatus] = useState<'idle' | 'checking' | 'available' | 'taken' | 'invalid'>('idle')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    getPlans()
      .then((data) => {
        setPlans(data)
        const match = data.find((p) => p.name.toLowerCase() === preselectedPlan.toLowerCase())
        if (match) setSelectedPlanId(match.id)
        else if (data.length > 0) setSelectedPlanId(data[0].id)
      })
      .catch(() => setError('Failed to load plans. Please try again later.'))
  }, [preselectedPlan])

  const checkSubdomainAvailability = useCallback(async (value: string) => {
    if (value.length < 3) {
      setSubdomainStatus('invalid')
      return
    }
    if (!/^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$/.test(value)) {
      setSubdomainStatus('invalid')
      return
    }
    setSubdomainStatus('checking')
    try {
      const result = await checkSubdomain(value)
      setSubdomainStatus(result.available ? 'available' : 'taken')
    } catch {
      setSubdomainStatus('idle')
    }
  }, [])

  useEffect(() => {
    if (!subdomain) {
      setSubdomainStatus('idle')
      return
    }
    const timer = setTimeout(() => checkSubdomainAvailability(subdomain), 400)
    return () => clearTimeout(timer)
  }, [subdomain, checkSubdomainAvailability])

  const handleSubdomainChange = (value: string) => {
    setSubdomain(value.toLowerCase().replace(/[^a-z0-9-]/g, ''))
  }

  const handlePharmacyNameChange = (value: string) => {
    setPharmacyName(value)
    if (!subdomain || subdomain === pharmacyName.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '')) {
      handleSubdomainChange(value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, ''))
    }
  }

  const selectedPlan = plans.find((p) => p.id === selectedPlanId)
  const price = selectedPlan
    ? billingPeriod === 'Yearly' ? selectedPlan.priceYearly : selectedPlan.priceMonthly
    : 0

  const canSubmit =
    pharmacyName.trim().length >= 2 &&
    subdomainStatus === 'available' &&
    adminEmail.includes('@') &&
    adminFullName.trim().length >= 2 &&
    selectedPlanId &&
    !submitting

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!canSubmit) return

    setSubmitting(true)
    setError(null)

    try {
      const result = await createCheckout({
        pharmacyName: pharmacyName.trim(),
        subdomain,
        adminEmail: adminEmail.trim(),
        adminFullName: adminFullName.trim(),
        planId: selectedPlanId,
        billingPeriod,
      })
      window.location.href = result.checkoutUrl
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong. Please try again.')
      setSubmitting(false)
    }
  }

  return (
    <>
      <section className="bg-primary px-6 py-12 text-white">
        <div className="mx-auto max-w-2xl">
          <Link to="/pricing" className="mb-4 inline-flex items-center gap-1 text-sm opacity-70 hover:opacity-100">
            <ArrowLeft className="h-4 w-4" /> Back to Pricing
          </Link>
          <h1 className="text-3xl font-bold">Create Your Pharmacy</h1>
          <p className="mt-2 opacity-80">Fill in the details below and you&apos;ll be ready in minutes.</p>
        </div>
      </section>

      <section className="px-6 py-12">
        <form onSubmit={handleSubmit} className="mx-auto max-w-2xl space-y-8">
          {error && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
              {error}
            </div>
          )}

          {/* Plan Selection */}
          <div>
            <h2 className="mb-3 text-lg font-semibold text-primary">Select Your Plan</h2>
            <div className="mb-4 flex gap-2">
              <button
                type="button"
                onClick={() => setBillingPeriod('Monthly')}
                className={`rounded-lg px-4 py-2 text-sm font-medium transition ${
                  billingPeriod === 'Monthly' ? 'bg-primary text-white' : 'bg-surface text-gray-600'
                }`}
              >
                Monthly
              </button>
              <button
                type="button"
                onClick={() => setBillingPeriod('Yearly')}
                className={`rounded-lg px-4 py-2 text-sm font-medium transition ${
                  billingPeriod === 'Yearly' ? 'bg-primary text-white' : 'bg-surface text-gray-600'
                }`}
              >
                Yearly <span className="text-xs opacity-70">(Save 17%)</span>
              </button>
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              {plans.map((plan) => (
                <button
                  key={plan.id}
                  type="button"
                  onClick={() => setSelectedPlanId(plan.id)}
                  className={`rounded-lg border-2 p-4 text-left transition ${
                    selectedPlanId === plan.id ? 'border-highlight bg-highlight/5' : 'border-surface-dark hover:border-gray-300'
                  }`}
                >
                  <div className="font-semibold text-primary">{plan.name}</div>
                  <div className="text-2xl font-bold text-primary">
                    ${billingPeriod === 'Yearly' ? plan.priceYearly : plan.priceMonthly}
                    <span className="text-sm font-normal text-gray-500">/{billingPeriod === 'Yearly' ? 'year' : 'month'}</span>
                  </div>
                  <div className="mt-1 text-xs text-gray-500">Up to {plan.maxUsers} users</div>
                </button>
              ))}
            </div>
          </div>

          {/* Pharmacy Details */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-primary">Pharmacy Details</h2>

            <div>
              <label htmlFor="pharmacyName" className="mb-1 block text-sm font-medium text-gray-700">
                Pharmacy Name
              </label>
              <input
                id="pharmacyName"
                type="text"
                required
                minLength={2}
                maxLength={200}
                value={pharmacyName}
                onChange={(e) => handlePharmacyNameChange(e.target.value)}
                className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-highlight focus:outline-none focus:ring-2 focus:ring-highlight/20"
                placeholder="Smith Pharmacy"
              />
            </div>

            <div>
              <label htmlFor="subdomain" className="mb-1 block text-sm font-medium text-gray-700">
                Your Subdomain
              </label>
              <div className="flex items-center gap-0">
                <input
                  id="subdomain"
                  type="text"
                  required
                  minLength={3}
                  maxLength={63}
                  value={subdomain}
                  onChange={(e) => handleSubdomainChange(e.target.value)}
                  className="w-full rounded-l-lg border border-r-0 border-gray-300 px-4 py-2.5 focus:border-highlight focus:outline-none focus:ring-2 focus:ring-highlight/20"
                  placeholder="smith-pharmacy"
                />
                <span className="rounded-r-lg border border-gray-300 bg-surface px-3 py-2.5 text-sm text-gray-500">
                  .zenpharm.com.au
                </span>
              </div>
              <div className="mt-1 flex items-center gap-1 text-sm">
                {subdomainStatus === 'checking' && (
                  <><Loader2 className="h-3.5 w-3.5 animate-spin text-gray-400" /> <span className="text-gray-400">Checking...</span></>
                )}
                {subdomainStatus === 'available' && (
                  <><Check className="h-3.5 w-3.5 text-green-500" /> <span className="text-green-600">Available</span></>
                )}
                {subdomainStatus === 'taken' && (
                  <><X className="h-3.5 w-3.5 text-red-500" /> <span className="text-red-600">Already taken</span></>
                )}
                {subdomainStatus === 'invalid' && (
                  <><X className="h-3.5 w-3.5 text-red-500" /> <span className="text-red-600">Must be 3+ lowercase letters, numbers, and hyphens</span></>
                )}
              </div>
            </div>
          </div>

          {/* Admin Account */}
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-primary">Admin Account</h2>

            <div>
              <label htmlFor="adminFullName" className="mb-1 block text-sm font-medium text-gray-700">
                Full Name
              </label>
              <input
                id="adminFullName"
                type="text"
                required
                minLength={2}
                maxLength={200}
                value={adminFullName}
                onChange={(e) => setAdminFullName(e.target.value)}
                className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-highlight focus:outline-none focus:ring-2 focus:ring-highlight/20"
                placeholder="John Smith"
              />
            </div>

            <div>
              <label htmlFor="adminEmail" className="mb-1 block text-sm font-medium text-gray-700">
                Email Address
              </label>
              <input
                id="adminEmail"
                type="email"
                required
                maxLength={200}
                value={adminEmail}
                onChange={(e) => setAdminEmail(e.target.value)}
                className="w-full rounded-lg border border-gray-300 px-4 py-2.5 focus:border-highlight focus:outline-none focus:ring-2 focus:ring-highlight/20"
                placeholder="john@smithpharmacy.com.au"
              />
            </div>
          </div>

          {/* Summary + Submit */}
          <div className="rounded-lg bg-surface p-6">
            <div className="mb-4 flex items-center justify-between">
              <span className="font-medium text-gray-700">
                {selectedPlan?.name ?? 'Select a plan'} — {billingPeriod}
              </span>
              <span className="text-2xl font-bold text-primary">
                ${price}<span className="text-sm font-normal text-gray-500">/{billingPeriod === 'Yearly' ? 'year' : 'month'}</span>
              </span>
            </div>
            <button
              type="submit"
              disabled={!canSubmit}
              className="w-full rounded-lg bg-highlight px-6 py-3 font-semibold text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {submitting ? (
                <span className="inline-flex items-center gap-2">
                  <Loader2 className="h-4 w-4 animate-spin" /> Processing...
                </span>
              ) : (
                'Proceed to Payment'
              )}
            </button>
            <p className="mt-3 text-center text-xs text-gray-500">
              You&apos;ll be redirected to Stripe for secure payment processing.
            </p>
          </div>
        </form>
      </section>
    </>
  )
}
