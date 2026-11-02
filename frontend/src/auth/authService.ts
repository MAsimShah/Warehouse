import { authClient } from '../api/authClient'
import { CSRF_HEADER } from '../config'
import { sessionStore } from './sessionStore'
import type { AuthResponse, AuthUser } from './types'

// Non-secret hint only ("a session may exist"). It is NOT a token. It avoids a pointless
// refresh call on every page load for visitors who are logged out.
const SESSION_HINT_KEY = 'wh.session'

function setHint(on: boolean) {
  try {
    if (on) localStorage.setItem(SESSION_HINT_KEY, '1')
    else localStorage.removeItem(SESSION_HINT_KEY)
  } catch {
    // Storage can be blocked. The app still works without the hint.
  }
}

function hasHint(): boolean {
  try {
    return localStorage.getItem(SESSION_HINT_KEY) === '1'
  } catch {
    return true
  }
}

// rememberMe = true keeps the user signed in after the browser is closed (until they sign out or are away 30 days).
export async function login(email: string, password: string, rememberMe: boolean): Promise<AuthUser> {
  const { data } = await authClient.post<AuthResponse>('/auth/login', { email: email.trim(), password, rememberMe })
  sessionStore.setSession(data)
  setHint(true)
  return data.user
}

// Refresh tokens rotate: each token works once. Two refresh calls at the same moment would make the
// second one look like token theft and end all sessions. So:
//  1. calls inside this tab share one in-flight request, and
//  2. calls across browser tabs are serialized with the Web Locks API.
let inflightRefresh: Promise<AuthResponse> | null = null

async function runExclusive<T>(task: () => Promise<T>): Promise<T> {
  if (typeof navigator !== 'undefined' && 'locks' in navigator) {
    return navigator.locks.request('wh-auth-refresh', task)
  }
  return task()
}

export function refreshSession(): Promise<AuthResponse> {
  if (!inflightRefresh) {
    inflightRefresh = runExclusive(async () => {
      const { data } = await authClient.post<AuthResponse>('/auth/refresh', null, { headers: CSRF_HEADER })
      sessionStore.setSession(data)
      setHint(true)
      return data
    }).finally(() => {
      inflightRefresh = null
    })
  }
  return inflightRefresh
}

// Runs once when the app starts: restores the session from the refresh cookie.
let bootstrapped = false

export async function bootstrapSession(): Promise<void> {
  if (bootstrapped) return
  bootstrapped = true

  if (!hasHint()) {
    sessionStore.clear()
    return
  }

  try {
    await refreshSession()
  } catch {
    setHint(false)
    sessionStore.clear()
  }
}

export async function logout(): Promise<void> {
  try {
    await authClient.post('/auth/logout', null, { headers: CSRF_HEADER })
  } catch {
    // Even if the server cannot be reached, the local session is always cleared.
  } finally {
    setHint(false)
    sessionStore.clear()
  }
}

export function endSessionLocally(): void {
  setHint(false)
  sessionStore.clear()
}
