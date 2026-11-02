import axios, { type InternalAxiosRequestConfig } from 'axios'
import { API_BASE_URL, REQUEST_TIMEOUT_MS } from '../config'
import { endSessionLocally, refreshSession } from '../auth/authService'
import { sessionStore } from '../auth/sessionStore'

// Client for all protected API calls. It adds the access token and, when the API answers 401
// (token expired), refreshes the session once and repeats the request.
export const api = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
  timeout: REQUEST_TIMEOUT_MS,
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const token = sessionStore.getAccessToken()
  if (token) config.headers.set('Authorization', `Bearer ${token}`)
  return config
})

type RetriableConfig = InternalAxiosRequestConfig & { _retried?: boolean }

api.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    if (!axios.isAxiosError(error)) return Promise.reject(error)

    const original = error.config as RetriableConfig | undefined
    if (error.response?.status !== 401 || !original || original._retried) {
      return Promise.reject(error)
    }

    original._retried = true
    try {
      const session = await refreshSession()
      original.headers.set('Authorization', `Bearer ${session.accessToken}`)
      return api(original)
    } catch (refreshError) {
      // The refresh cookie is gone, expired or revoked: the user must log in again.
      if (axios.isAxiosError(refreshError) && [400, 401].includes(refreshError.response?.status ?? 0)) {
        endSessionLocally()
      }
      return Promise.reject(error)
    }
  },
)
