import { apiFetch } from './client'

export interface TenantSummary {
  id: string
  name: string
  subdomain: string
  isActive: boolean
  createdAt: string
}

export interface PendingSignup {
  id: string
  pharmacyName: string
  subdomain: string
  adminEmail: string
  status: string
  createdAt: string
}

export interface PbsSyncResult {
  totalProducts: number
  matched: number
  updated: number
}

export interface PbsSummary {
  totalProducts: number
  withPbsCode: number
  withoutPbsCode: number
}

export function getTenants(): Promise<TenantSummary[]> {
  return apiFetch<TenantSummary[]>('/api/platform/tenants')
}

export function getPendingSignups(): Promise<PendingSignup[]> {
  return apiFetch<PendingSignup[]>('/api/platform/pending-signups')
}

export function runPbsSync(): Promise<PbsSyncResult> {
  return apiFetch<PbsSyncResult>('/api/platform/pbs-sync', { method: 'POST' })
}

export function getPbsSummary(): Promise<PbsSummary> {
  return apiFetch<PbsSummary>('/api/platform/pbs-summary')
}
