import { useState, useRef, useEffect, useCallback } from 'react'
import { MessageCircle, X, Send, Loader2, Wrench } from 'lucide-react'
import ReactMarkdown from 'react-markdown'
import { streamChatMessage, type ChatMessage, type StreamEvent } from '../../api/chat'

interface DisplayMessage {
  role: 'user' | 'assistant'
  content: string
  toolName?: string
}

export default function AiConsultant() {
  const [isOpen, setIsOpen] = useState(false)
  const [messages, setMessages] = useState<DisplayMessage[]>([])
  const [input, setInput] = useState('')
  const [streaming, setStreaming] = useState(false)
  const [sessionToken, setSessionToken] = useState<string>()
  const [activeTool, setActiveTool] = useState<string>()
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const abortRef = useRef<AbortController>(null)

  const scrollToBottom = useCallback(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [])

  useEffect(() => { scrollToBottom() }, [messages, scrollToBottom])

  const handleSend = async () => {
    const text = input.trim()
    if (!text || streaming) return

    setInput('')
    const userMsg: DisplayMessage = { role: 'user', content: text }
    setMessages(prev => [...prev, userMsg])
    setStreaming(true)
    setActiveTool(undefined)

    const history: ChatMessage[] = messages.map(m => ({
      role: m.role,
      content: m.content,
    }))

    const controller = new AbortController()
    abortRef.current = controller

    let assistantReply = ''

    try {
      setMessages(prev => [...prev, { role: 'assistant', content: '' }])

      for await (const event of streamChatMessage(
        { message: text, history, sessionToken },
        controller.signal,
      )) {
        handleStreamEvent(event, (chunk) => {
          assistantReply += chunk
          setMessages(prev => {
            const updated = [...prev]
            updated[updated.length - 1] = { role: 'assistant', content: assistantReply }
            return updated
          })
        })
      }
    } catch (err) {
      if ((err as Error).name !== 'AbortError') {
        const errorMsg = (err as Error).message || 'Something went wrong. Please try again.'
        setMessages(prev => {
          const updated = [...prev]
          updated[updated.length - 1] = { role: 'assistant', content: errorMsg }
          return updated
        })
      }
    } finally {
      setStreaming(false)
      setActiveTool(undefined)
    }
  }

  const handleStreamEvent = (event: StreamEvent, appendText: (text: string) => void) => {
    switch (event.type) {
      case 'text':
        if (event.text) appendText(event.text)
        break
      case 'tool_start':
        setActiveTool(event.toolName)
        break
      case 'tool_result':
        setActiveTool(undefined)
        break
      case 'done':
        if (event.sessionToken) setSessionToken(event.sessionToken)
        break
      case 'error':
        appendText(event.error ?? 'An error occurred.')
        break
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  return (
    <>
      {isOpen && (
        <div className="fixed bottom-24 right-6 z-50 flex w-96 flex-col rounded-2xl border border-surface-dark bg-white shadow-2xl"
          style={{ maxHeight: 'calc(100vh - 140px)' }}>
          {/* Header */}
          <div className="flex items-center justify-between rounded-t-2xl bg-primary px-6 py-4 text-white">
            <h3 className="font-semibold">AI Consultant</h3>
            <button
              onClick={() => setIsOpen(false)}
              className="rounded-full p-1 transition-colours hover:bg-white/20"
            >
              <X className="h-5 w-5" />
            </button>
          </div>

          {/* Messages */}
          <div className="flex-1 overflow-y-auto p-4" style={{ minHeight: 300 }}>
            {messages.length === 0 && (
              <div className="flex h-full items-center justify-center text-center text-sm text-gray-400">
                Ask me anything about our services!
              </div>
            )}
            {messages.map((msg, i) => (
              <div key={i} className={`mb-3 flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                <div className={`max-w-[80%] rounded-xl px-4 py-2.5 text-sm ${
                  msg.role === 'user'
                    ? 'bg-primary text-white'
                    : 'bg-surface text-gray-800'
                }`}>
                  {msg.role === 'assistant' ? (
                    <div className="prose prose-sm max-w-none">
                      <ReactMarkdown>{msg.content || '...'}</ReactMarkdown>
                    </div>
                  ) : (
                    msg.content
                  )}
                </div>
              </div>
            ))}
            {activeTool && (
              <div className="mb-3 flex items-center gap-2 text-xs text-gray-500">
                <Wrench className="h-3 w-3 animate-spin" />
                Using {activeTool}...
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>

          {/* Input */}
          <div className="border-t border-surface-dark p-3">
            <div className="flex items-center gap-2">
              <input
                type="text"
                value={input}
                onChange={e => setInput(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="Type your message..."
                disabled={streaming}
                className="flex-1 rounded-lg border border-surface-dark bg-surface px-3 py-2 text-sm outline-none transition-colours focus:border-accent disabled:opacity-50"
              />
              <button
                onClick={handleSend}
                disabled={streaming || !input.trim()}
                className="flex h-9 w-9 items-center justify-center rounded-lg bg-highlight text-white transition-opacity hover:opacity-90 disabled:opacity-50"
              >
                {streaming ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
              </button>
            </div>
          </div>
        </div>
      )}

      <button
        onClick={() => setIsOpen(prev => !prev)}
        className="fixed bottom-6 right-6 z-50 flex h-14 w-14 items-center justify-center rounded-full bg-highlight text-white shadow-lg transition-transform hover:scale-105"
        aria-label="Open AI consultant"
      >
        <MessageCircle className="h-6 w-6" />
      </button>
    </>
  )
}
