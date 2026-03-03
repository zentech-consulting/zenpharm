import { detectSubdomain } from './tenant'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

/**
 * Shared fetch wrapper for the public frontend.
 * Automatically injects X-Tenant-Subdomain header on every request.
 */
export async function apiFetch(path: string, init?: RequestInit): Promise<Response> {
  const headers = new Headers(init?.headers)

  const subdomain = detectSubdomain()
  if (subdomain) {
    headers.set('X-Tenant-Subdomain', subdomain)
  }

  if (!headers.has('Content-Type') && init?.body) {
    headers.set('Content-Type', 'application/json')
  }

  const url = path.startsWith('http') ? path : `${API_BASE}${path}`

  return fetch(url, { ...init, headers })
}
