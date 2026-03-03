const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

export interface PlanSummary {
  id: string
  name: string
  priceMonthly: number
  priceYearly: number
  features: string | null
  maxUsers: number
  maxProducts: number
}

export interface CheckoutResponse {
  checkoutUrl: string
  sessionId: string
}

export interface SignupStatusResponse {
  status: string
  tenantId: string | null
  adminPanelUrl: string | null
  message: string | null
}

export interface SubdomainCheckResponse {
  subdomain: string
  available: boolean
}

export async function getPlans(): Promise<PlanSummary[]> {
  const res = await fetch(`${API_BASE}/api/signup/plans`)
  if (!res.ok) throw new Error('Failed to fetch plans')
  return res.json()
}

export async function checkSubdomain(subdomain: string): Promise<SubdomainCheckResponse> {
  const res = await fetch(`${API_BASE}/api/signup/check-subdomain/${encodeURIComponent(subdomain)}`)
  if (!res.ok) throw new Error('Failed to check subdomain')
  return res.json()
}

export async function createCheckout(data: {
  pharmacyName: string
  subdomain: string
  adminEmail: string
  adminFullName: string
  planId: string
  billingPeriod: string
}): Promise<CheckoutResponse> {
  const res = await fetch(`${API_BASE}/api/signup/checkout`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: 'Checkout failed' }))
    throw new Error(err.error ?? 'Checkout failed')
  }
  return res.json()
}

export async function getSignupStatus(sessionId: string): Promise<SignupStatusResponse> {
  const res = await fetch(`${API_BASE}/api/signup/status/${encodeURIComponent(sessionId)}`)
  if (!res.ok) throw new Error('Failed to fetch status')
  return res.json()
}
