import { describe, it, expect, vi, beforeEach } from 'vitest'

describe('signup API client', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn())
  })

  it('getPlans calls correct endpoint', async () => {
    const mockPlans = [
      { id: '1', name: 'Basic', priceMonthly: 79, priceYearly: 790, features: null, maxUsers: 5, maxProducts: 500 },
    ]
    ;(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockPlans),
    })

    const { getPlans } = await import('../signup')
    const result = await getPlans()

    expect(fetch).toHaveBeenCalledWith('/api/signup/plans')
    expect(result).toEqual(mockPlans)
  })

  it('checkSubdomain encodes subdomain in URL', async () => {
    ;(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({ subdomain: 'test', available: true }),
    })

    const { checkSubdomain } = await import('../signup')
    const result = await checkSubdomain('test')

    expect(fetch).toHaveBeenCalledWith('/api/signup/check-subdomain/test')
    expect(result.available).toBe(true)
  })

  it('createCheckout sends POST with correct body', async () => {
    const checkoutResp = { checkoutUrl: 'https://checkout.stripe.com/test', sessionId: 'cs_test_123' }
    ;(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(checkoutResp),
    })

    const { createCheckout } = await import('../signup')
    const result = await createCheckout({
      pharmacyName: 'Test Pharmacy',
      subdomain: 'test-pharmacy',
      adminEmail: 'admin@test.com',
      adminFullName: 'Test Admin',
      planId: 'plan-123',
      billingPeriod: 'Monthly',
    })

    expect(fetch).toHaveBeenCalledWith(
      '/api/signup/checkout',
      expect.objectContaining({
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    expect(result.sessionId).toBe('cs_test_123')
  })

  it('createCheckout throws on error response', async () => {
    ;(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: false,
      json: () => Promise.resolve({ error: 'Subdomain already taken' }),
    })

    const { createCheckout } = await import('../signup')
    await expect(
      createCheckout({
        pharmacyName: 'Test',
        subdomain: 'taken',
        adminEmail: 'a@b.com',
        adminFullName: 'Test',
        planId: 'p1',
        billingPeriod: 'Monthly',
      }),
    ).rejects.toThrow('Subdomain already taken')
  })

  it('getSignupStatus calls correct endpoint', async () => {
    ;(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve({ status: 'active', tenantId: 't1', adminPanelUrl: 'https://admin.test.zenpharm.com.au', message: 'Ready!' }),
    })

    const { getSignupStatus } = await import('../signup')
    const result = await getSignupStatus('cs_test_123')

    expect(fetch).toHaveBeenCalledWith('/api/signup/status/cs_test_123')
    expect(result.status).toBe('active')
  })
})
