// Base URL of the Warehouse API. The default "/api" is same-origin: in development Vite forwards it to the
// backend (see vite.config.ts), in production the web server does. Same-origin is required so the browser keeps
// the SameSite=Strict refresh cookie. Override at build time with VITE_API_BASE_URL.
export const API_BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? '/api'

// How long the browser waits for the API. Longer in development so a paused debugger does not
// make the browser give up (which cancels the request on the server).
export const REQUEST_TIMEOUT_MS = import.meta.env.DEV ? 300_000 : 15_000

// Sent on cookie-based auth calls (refresh, logout). The API requires it as CSRF protection.
export const CSRF_HEADER = { 'X-Requested-With': 'XMLHttpRequest' } as const
