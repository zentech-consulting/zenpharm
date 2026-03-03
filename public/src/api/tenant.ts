/**
 * Detects the tenant subdomain from the current hostname.
 *
 * Priority:
 * 1. VITE_TENANT_SUBDOMAIN env var (build-time override for Azure SWA demo)
 * 2. Extract from window.location.hostname
 *
 * Handles compound TLDs (.com.au, .co.uk) and localhost fallback.
 */
export function detectSubdomain(): string | null {
  const envOverride = import.meta.env.VITE_TENANT_SUBDOMAIN
  if (envOverride) return envOverride

  if (typeof window === 'undefined') return null

  const hostname = window.location.hostname

  // localhost or IP → no subdomain detection possible
  if (hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '::1') {
    return null
  }

  const parts = hostname.split('.')

  // Compound TLDs: .com.au, .co.uk, .co.nz, etc.
  const tldSegments = getTldSegmentCount(parts)
  const domainParts = parts.length - tldSegments

  // Naked domain (no subdomain)
  if (domainParts <= 1) return null

  const subdomain = parts[0]

  // Reserved subdomains
  const reserved = new Set(['www', 'api', 'admin', 'mail', 'ftp'])
  if (reserved.has(subdomain.toLowerCase())) return null

  return subdomain
}

function getTldSegmentCount(parts: string[]): number {
  if (parts.length < 2) return 1

  const last = parts[parts.length - 1]
  const secondLast = parts[parts.length - 2]

  const compoundTldCountries = new Set(['au', 'uk', 'nz', 'in', 'za', 'jp', 'br', 'kr'])
  const compoundTldPrefixes = new Set(['com', 'co', 'org', 'net', 'edu', 'gov'])

  if (compoundTldCountries.has(last) && compoundTldPrefixes.has(secondLast)) {
    return 2
  }

  return 1
}
