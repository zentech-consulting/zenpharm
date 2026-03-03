import { apiFetch } from './client'

export interface BrandingData {
  displayName: string
  shortName: string | null
  logoUrl: string | null
  faviconUrl: string | null
  primaryColour: string
  secondaryColour: string | null
  accentColour: string | null
  highlightColour: string | null
  tagline: string | null
  contactEmail: string | null
  contactPhone: string | null
  abn: string | null
  addressLine1: string | null
  addressLine2: string | null
  suburb: string | null
  state: string | null
  postcode: string | null
  businessHoursJson: string | null
  plan: string
}

export async function fetchBranding(): Promise<BrandingData> {
  return apiFetch<BrandingData>('/api/branding', { skipAuth: true })
}
