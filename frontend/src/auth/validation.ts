export const EMAIL_MAX = 254
export const PASSWORD_MAX = 128

export interface LoginErrors {
  email?: string
  password?: string
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

// Quick checks for a better user experience. The API validates everything again, so these are not a security boundary.
export function validateLogin(email: string, password: string): LoginErrors {
  const errors: LoginErrors = {}
  const trimmed = email.trim()

  if (!trimmed) errors.email = 'Email is required.'
  else if (trimmed.length > EMAIL_MAX || !EMAIL_PATTERN.test(trimmed)) errors.email = 'Enter a valid email address.'

  if (!password) errors.password = 'Password is required.'
  else if (password.length > PASSWORD_MAX) errors.password = `Password must be at most ${PASSWORD_MAX} characters.`

  return errors
}

// Only allow redirects to pages inside this app. Blocks open redirects such as "//evil.com" or "https://evil.com".
export function safeRedirectPath(value: unknown): string {
  if (typeof value !== 'string') return '/'
  if (!value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) return '/'
  if (value.startsWith('/login')) return '/'
  return value
}
