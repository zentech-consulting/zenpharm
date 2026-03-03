import { useState, useEffect, useRef } from 'react'
import { useSearchParams, Link } from 'react-router-dom'
import { Check, Loader2, AlertTriangle, ExternalLink } from 'lucide-react'
import { getSignupStatus } from '../api/signup'
import type { SignupStatusResponse } from '../api/signup'

export default function SignupSuccessPage() {
  const [searchParams] = useSearchParams()
  const sessionId = searchParams.get('session_id')

  const [status, setStatus] = useState<SignupStatusResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    if (!sessionId) {
      setError('No session ID found. Please check your signup link.')
      return
    }

    const poll = async () => {
      try {
        const result = await getSignupStatus(sessionId)
        setStatus(result)

        if (result.status === 'active' || result.status === 'failed' || result.status === 'expired') {
          if (intervalRef.current) {
            clearInterval(intervalRef.current)
            intervalRef.current = null
          }
        }
      } catch {
        setError('Failed to check status. Please refresh the page.')
      }
    }

    poll()
    intervalRef.current = setInterval(poll, 3000)

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current)
    }
  }, [sessionId])

  const isLoading = !status && !error
  const isProvisioning = status?.status === 'pending_payment' || status?.status === 'provisioning'
  const isActive = status?.status === 'active'
  const isFailed = status?.status === 'failed' || status?.status === 'expired'

  return (
    <>
      <section className="bg-primary px-6 py-12 text-white">
        <div className="mx-auto max-w-2xl text-center">
          <h1 className="text-3xl font-bold">
            {isActive ? 'Your Pharmacy is Ready!' : 'Setting Up Your Pharmacy'}
          </h1>
        </div>
      </section>

      <section className="px-6 py-16">
        <div className="mx-auto max-w-lg text-center">
          {error && (
            <div className="mb-8 rounded-lg border border-red-200 bg-red-50 p-6">
              <AlertTriangle className="mx-auto mb-3 h-10 w-10 text-red-400" />
              <p className="text-red-700">{error}</p>
              <Link
                to="/pricing"
                className="mt-4 inline-block rounded-lg bg-primary px-6 py-2 text-sm font-semibold text-white"
              >
                Back to Pricing
              </Link>
            </div>
          )}

          {isLoading && !error && (
            <div className="py-12">
              <Loader2 className="mx-auto h-12 w-12 animate-spin text-primary" />
              <p className="mt-4 text-gray-500">Checking your signup status...</p>
            </div>
          )}

          {isProvisioning && (
            <div className="py-8">
              <div className="mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full bg-blue-50">
                <Loader2 className="h-10 w-10 animate-spin text-primary" />
              </div>
              <h2 className="mb-2 text-xl font-semibold text-primary">
                {status?.status === 'pending_payment' ? 'Confirming Payment...' : 'Building Your Pharmacy...'}
              </h2>
              <p className="text-gray-500">{status?.message ?? 'This usually takes about a minute.'}</p>
              <div className="mt-8 space-y-3">
                <ProgressStep label="Payment confirmed" done={status?.status !== 'pending_payment'} />
                <ProgressStep label="Creating your database" done={false} active={status?.status === 'provisioning'} />
                <ProgressStep label="Importing product catalogue" done={false} />
                <ProgressStep label="Setting up admin account" done={false} />
              </div>
            </div>
          )}

          {isActive && (
            <div className="py-8">
              <div className="mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full bg-green-50">
                <Check className="h-10 w-10 text-green-500" />
              </div>
              <h2 className="mb-2 text-xl font-semibold text-green-700">All Set!</h2>
              <p className="mb-8 text-gray-600">{status?.message}</p>
              {status?.adminPanelUrl && (
                <a
                  href={status.adminPanelUrl}
                  className="inline-flex items-center gap-2 rounded-lg bg-highlight px-8 py-3 font-semibold text-white transition-opacity hover:opacity-90"
                >
                  Go to Admin Panel <ExternalLink className="h-4 w-4" />
                </a>
              )}
              <p className="mt-4 text-sm text-gray-500">
                Check your email for login credentials.
              </p>
            </div>
          )}

          {isFailed && (
            <div className="py-8">
              <div className="mx-auto mb-6 flex h-20 w-20 items-center justify-center rounded-full bg-red-50">
                <AlertTriangle className="h-10 w-10 text-red-400" />
              </div>
              <h2 className="mb-2 text-xl font-semibold text-red-700">Something Went Wrong</h2>
              <p className="mb-8 text-gray-600">{status?.message}</p>
              <Link
                to="/contact"
                className="inline-block rounded-lg bg-primary px-8 py-3 font-semibold text-white transition-opacity hover:opacity-90"
              >
                Contact Support
              </Link>
            </div>
          )}
        </div>
      </section>
    </>
  )
}

function ProgressStep({ label, done, active }: { label: string; done: boolean; active?: boolean }) {
  return (
    <div className="flex items-center gap-3">
      {done ? (
        <div className="flex h-6 w-6 items-center justify-center rounded-full bg-green-500">
          <Check className="h-3.5 w-3.5 text-white" />
        </div>
      ) : active ? (
        <Loader2 className="h-6 w-6 animate-spin text-primary" />
      ) : (
        <div className="h-6 w-6 rounded-full border-2 border-gray-200" />
      )}
      <span className={`text-sm ${done ? 'font-medium text-green-700' : active ? 'font-medium text-primary' : 'text-gray-400'}`}>
        {label}
      </span>
    </div>
  )
}
