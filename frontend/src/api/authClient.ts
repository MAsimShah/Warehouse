import axios from 'axios'
import { API_BASE_URL, REQUEST_TIMEOUT_MS } from '../config'

// Plain client with NO interceptors. Used for login, refresh and logout so they can never loop.
// withCredentials lets the browser send and store the HttpOnly refresh cookie (needs CORS credentials on the API).
export const authClient = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
  timeout: REQUEST_TIMEOUT_MS,
  headers: { 'Content-Type': 'application/json' },
})
