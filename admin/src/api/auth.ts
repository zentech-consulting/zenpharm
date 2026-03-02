import { apiFetch } from './client'

interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: {
    id: string
    username: string
    email: string | null
    displayName: string | null
    role: string
  }
}

export async function login(username: string, password: string): Promise<LoginResponse> {
  return apiFetch<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
    skipAuth: true,
  })
}

export async function refreshToken(token: string): Promise<{ accessToken: string; refreshToken: string; expiresAt: string }> {
  return apiFetch('/api/auth/refresh', {
    method: 'POST',
    body: JSON.stringify({ refreshToken: token }),
    skipAuth: true,
  })
}

export async function logout(): Promise<void> {
  const token = localStorage.getItem('refreshToken')
  if (token) {
    await apiFetch('/api/auth/logout', {
      method: 'POST',
      body: JSON.stringify({ refreshToken: token }),
    })
  }
  localStorage.removeItem('accessToken')
  localStorage.removeItem('refreshToken')
}

export async function getCurrentUser(): Promise<LoginResponse['user']> {
  return apiFetch('/api/auth/me')
}

// --- Session Management ---

export interface ActiveSession {
  id: string
  username: string
  createdByIp: string | null
  createdAt: string
  lastUsedAt: string | null
}

export interface SessionSummary {
  activeSessions: number
  maxSessions: number
  planName: string
}

export interface SessionListResponse {
  sessions: ActiveSession[]
  maxSessions: number
}

export async function fetchActiveSessions(): Promise<SessionListResponse> {
  return apiFetch<SessionListResponse>('/api/auth/sessions')
}

export async function fetchSessionSummary(): Promise<SessionSummary> {
  return apiFetch<SessionSummary>('/api/auth/sessions/summary')
}

export async function revokeSession(sessionId: string): Promise<void> {
  return apiFetch<void>(`/api/auth/sessions/${sessionId}`, { method: 'DELETE' })
}
