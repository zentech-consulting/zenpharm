import { apiFetch } from './client'

export interface KnowledgeEntry {
  id: string
  title: string
  content: string
  category: string
  tags: string[]
  createdAt: string
  updatedAt: string
}

interface KnowledgeList { items: KnowledgeEntry[]; totalCount: number }

export const fetchKnowledgeEntries = (page = 1, pageSize = 20, category?: string) =>
  apiFetch<KnowledgeList>(`/api/knowledge?page=${page}&pageSize=${pageSize}${category ? `&category=${encodeURIComponent(category)}` : ''}`)

export const fetchKnowledgeEntry = (id: string) => apiFetch<KnowledgeEntry>(`/api/knowledge/${id}`)

export const createKnowledgeEntry = (data: { title: string; content: string; category: string; tags?: string[] }) =>
  apiFetch<KnowledgeEntry>('/api/knowledge', { method: 'POST', body: JSON.stringify(data) })

export const updateKnowledgeEntry = (id: string, data: { title: string; content: string; category: string; tags?: string[] }) =>
  apiFetch<KnowledgeEntry>(`/api/knowledge/${id}`, { method: 'PUT', body: JSON.stringify(data) })

export const deleteKnowledgeEntry = (id: string) =>
  apiFetch<void>(`/api/knowledge/${id}`, { method: 'DELETE' })
