import axios from 'axios'

// Field errors from the API (ProblemDetails "errors"), with the first letter lower-cased to match our field names.
// Returns the first message per field. `message` is set when there are no field errors (network, 403, 429, ...).
export function readApiErrors(error: unknown): { fields: Record<string, string>; message: string | null } {
  if (axios.isAxiosError(error)) {
    if (!error.response) return { fields: {}, message: 'Cannot reach the server. Check your connection and try again.' }

    const raw = (error.response.data as { errors?: Record<string, string[]> } | undefined)?.errors
    if (raw) {
      const fields: Record<string, string> = {}
      for (const [key, messages] of Object.entries(raw)) {
        fields[key.charAt(0).toLowerCase() + key.slice(1)] = messages.join(' ')
      }
      return { fields, message: null }
    }
    if (error.response.status === 403) return { fields: {}, message: 'You do not have permission to do this.' }
    if (error.response.status === 404) return { fields: {}, message: 'This item no longer exists.' }
    if (error.response.status === 413) return { fields: {}, message: 'The photo is too large.' }
    if (error.response.status === 429) return { fields: {}, message: 'Too many attempts. Please wait a minute and try again.' }
  }
  return { fields: {}, message: 'Something went wrong. Please try again.' }
}
