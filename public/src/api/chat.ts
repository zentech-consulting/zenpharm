import { apiFetch } from './client'

export interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
}

export interface AiChatRequest {
  message: string
  history?: ChatMessage[]
  sessionToken?: string
}

export interface AiChatResponse {
  reply: string
  model?: string
  sessionToken?: string
}

export interface StreamEvent {
  type: 'text' | 'tool_start' | 'tool_end' | 'tool_result' | 'done' | 'error'
  text?: string
  toolName?: string
  model?: string
  error?: string
  sessionToken?: string
}

export async function sendChatMessage(request: AiChatRequest): Promise<AiChatResponse> {
  const res = await apiFetch(`/api/ai-chat`, {
    method: 'POST',
    body: JSON.stringify(request),
  })

  if (res.status === 429) {
    throw new Error('Rate limit exceeded. Please try again shortly.')
  }

  if (!res.ok) {
    throw new Error(`Chat request failed: ${res.status}`)
  }

  return res.json()
}

export async function* streamChatMessage(
  request: AiChatRequest,
  signal?: AbortSignal,
): AsyncGenerator<StreamEvent> {
  const res = await apiFetch(`/api/ai-chat/stream`, {
    method: 'POST',
    body: JSON.stringify(request),
    signal,
  })

  if (res.status === 429) {
    throw new Error('Rate limit exceeded. Please try again shortly.')
  }

  if (!res.ok) {
    throw new Error(`Stream request failed: ${res.status}`)
  }

  const reader = res.body?.getReader()
  if (!reader) throw new Error('No response body')

  const decoder = new TextDecoder()
  let buffer = ''

  try {
    while (true) {
      const { done, value } = await reader.read()
      if (done) break

      buffer += decoder.decode(value, { stream: true })
      const lines = buffer.split('\n')
      buffer = lines.pop() ?? ''

      let currentEvent = ''
      for (const line of lines) {
        if (line.startsWith('event: ')) {
          currentEvent = line.slice(7)
        } else if (line.startsWith('data: ') && currentEvent) {
          try {
            const data = JSON.parse(line.slice(6)) as StreamEvent
            yield { ...data, type: currentEvent as StreamEvent['type'] }
          } catch {
            // Skip malformed data lines
          }
          currentEvent = ''
        }
      }
    }
  } finally {
    reader.releaseLock()
  }
}
