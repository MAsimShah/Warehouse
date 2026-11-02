import { useSyncExternalStore } from 'react'
import type { AuthResponse, AuthUser } from './types'

export interface SessionState {
  status: 'loading' | 'authenticated' | 'anonymous'
  user: AuthUser | null
}

// The access token lives ONLY in this module variable (memory).
// It is never written to localStorage, sessionStorage, cookies or the URL, so XSS cannot read it from storage
// and it disappears on page reload (the HttpOnly refresh cookie restores the session).
let accessToken: string | null = null

let state: SessionState = { status: 'loading', user: null }
const listeners = new Set<() => void>()

function emit(next: SessionState) {
  state = next
  listeners.forEach((listener) => listener())
}

export const sessionStore = {
  getAccessToken: (): string | null => accessToken,

  setSession(auth: AuthResponse) {
    accessToken = auth.accessToken
    emit({ status: 'authenticated', user: auth.user })
  },

  // Replaces the signed-in user (for example after a new profile photo) and keeps the access token.
  updateUser(user: AuthUser) {
    emit({ status: 'authenticated', user })
  },

  clear() {
    accessToken = null
    emit({ status: 'anonymous', user: null })
  },

  subscribe(listener: () => void) {
    listeners.add(listener)
    return () => {
      listeners.delete(listener)
    }
  },

  getSnapshot: (): SessionState => state,
}

export function useSession(): SessionState {
  return useSyncExternalStore(sessionStore.subscribe, sessionStore.getSnapshot)
}
