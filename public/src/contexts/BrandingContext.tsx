import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { fetchBranding, type BrandingData } from '../api/branding'

const defaultBranding: BrandingData = {
  displayName: 'ZenPharm',
  shortName: 'ZP',
  logoUrl: null,
  faviconUrl: null,
  primaryColour: '#1a1a2e',
  secondaryColour: '#16213e',
  accentColour: '#0f3460',
  highlightColour: '#e94560',
  tagline: null,
  contactEmail: null,
  contactPhone: null,
  abn: null,
  addressLine1: null,
  addressLine2: null,
  suburb: null,
  state: null,
  postcode: null,
  businessHoursJson: null,
  plan: 'Free',
}

interface BrandingContextValue {
  branding: BrandingData
  loading: boolean
}

const BrandingContext = createContext<BrandingContextValue>({
  branding: defaultBranding,
  loading: true,
})

export function BrandingProvider({ children }: { children: ReactNode }) {
  const [branding, setBranding] = useState<BrandingData>(defaultBranding)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        const data = await fetchBranding()
        if (cancelled) return
        setBranding(data)
        applyCssProperties(data)
        document.title = data.displayName
      } catch {
        // Keep defaults on failure
        applyCssProperties(defaultBranding)
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    load()
    return () => { cancelled = true }
  }, [])

  return (
    <BrandingContext.Provider value={{ branding, loading }}>
      {children}
    </BrandingContext.Provider>
  )
}

export function useBranding(): BrandingContextValue {
  return useContext(BrandingContext)
}

function applyCssProperties(b: BrandingData) {
  const root = document.documentElement.style
  root.setProperty('--color-primary', b.primaryColour)
  if (b.secondaryColour) root.setProperty('--color-secondary', b.secondaryColour)
  if (b.accentColour) root.setProperty('--color-accent', b.accentColour)
  if (b.highlightColour) root.setProperty('--color-highlight', b.highlightColour)
}
