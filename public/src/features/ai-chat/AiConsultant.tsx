import { useState } from 'react'
import { MessageCircle, X } from 'lucide-react'

export default function AiConsultant() {
  const [isOpen, setIsOpen] = useState(false)

  return (
    <>
      {isOpen && (
        <div className="fixed bottom-24 right-6 z-50 w-96 rounded-2xl border border-surface-dark bg-white shadow-2xl">
          <div className="flex items-center justify-between rounded-t-2xl bg-primary px-6 py-4 text-white">
            <h3 className="font-semibold">AI Consultant</h3>
            <button
              onClick={() => setIsOpen(false)}
              className="rounded-full p-1 transition-colors hover:bg-white/20"
            >
              <X className="h-5 w-5" />
            </button>
          </div>
          <div className="flex h-80 items-center justify-center p-6 text-centre text-sm text-gray-500">
            AI chat interface coming soon.
            <br />
            Configure your AI knowledge base to enable this feature.
          </div>
        </div>
      )}

      <button
        onClick={() => setIsOpen((prev) => !prev)}
        className="fixed bottom-6 right-6 z-50 flex h-14 w-14 items-center justify-center rounded-full bg-highlight text-white shadow-lg transition-transform hover:scale-105"
        aria-label="Open AI consultant"
      >
        <MessageCircle className="h-6 w-6" />
      </button>
    </>
  )
}
