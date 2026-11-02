import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Basic security headers for the dev and preview servers.
// In production, set these (plus a Content-Security-Policy) on the web server that hosts the build.
const securityHeaders = {
  'X-Content-Type-Options': 'nosniff',
  'X-Frame-Options': 'DENY',
  'Referrer-Policy': 'no-referrer',
  'Permissions-Policy': 'geolocation=(), microphone=(), camera=()',
}

// Forward /api to the backend so the browser only talks to one origin (needed for the SameSite=Strict
// refresh cookie). "secure: false" accepts the self-signed .NET development certificate, so the
// certificate does not need to be trusted in the browser for local work.
const apiProxy = {
  '/api': { target: 'https://localhost:7072', secure: false },
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: { port: 5173, strictPort: true, headers: securityHeaders, proxy: apiProxy },
  preview: { port: 5173, strictPort: true, headers: securityHeaders, proxy: apiProxy },
})
