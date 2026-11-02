import { useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import axios from 'axios'
import { login } from '../auth/authService'
import { EMAIL_MAX, PASSWORD_MAX, safeRedirectPath, validateLogin, type LoginErrors } from '../auth/validation'

// Generic messages on purpose: never tell the user whether the email exists.
function toLoginMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    if (!error.response) return 'Cannot reach the server. Check your connection and try again.'
    switch (error.response.status) {
      case 400:
        return 'Please check your email and password and try again.'
      case 401:
        return 'Invalid email or password. After several failed attempts the account is locked for 15 minutes.'
      case 429:
        return 'Too many attempts. Please wait a minute and try again.'
    }
  }
  return 'Something went wrong. Please try again.'
}

export default function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const emailRef = useRef<HTMLInputElement>(null)
  const passwordRef = useRef<HTMLInputElement>(null)

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [rememberMe, setRememberMe] = useState(false)
  const [errors, setErrors] = useState<LoginErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (submitting) return

    const found = validateLogin(email, password)
    setErrors(found)
    setFormError(null)
    if (found.email) return emailRef.current?.focus()
    if (found.password) return passwordRef.current?.focus()

    setSubmitting(true)
    try {
      await login(email, password, rememberMe)
      const from = (location.state as { from?: unknown } | null)?.from
      navigate(safeRedirectPath(from), { replace: true })
    } catch (error) {
      setPassword('')
      setFormError(toLoginMessage(error))
      passwordRef.current?.focus()
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="page-center">
      <section className="auth-form" aria-labelledby="login-title">
        <header className="auth-header">
          <span className="auth-app-name">Warehouse</span>
          <h1 id="login-title">Sign in</h1>
          <p>Enter your email and password to continue.</p>
        </header>

        <form onSubmit={handleSubmit} noValidate>
          {formError && (
            <div className="form-message" role="alert">
              {formError}
            </div>
          )}

          <div className="field">
            <label htmlFor="email">Email</label>
            <input
              ref={emailRef}
              id="email"
              name="email"
              type="email"
              className="wh-input"
              autoComplete="username"
              inputMode="email"
              autoCapitalize="none"
              spellCheck={false}
              autoFocus
              maxLength={EMAIL_MAX}
              value={email}
              aria-invalid={!!errors.email}
              aria-describedby={errors.email ? 'email-error' : undefined}
              disabled={submitting}
              onChange={(e: ChangeEvent<HTMLInputElement>) => setEmail(e.target.value)}
            />
            {errors.email && (
              <small id="email-error" className="field-error">
                {errors.email}
              </small>
            )}
          </div>

          <div className="field">
            <label htmlFor="password">Password</label>
            <div className="password-wrap">
              <input
                ref={passwordRef}
                id="password"
                name="password"
                type={showPassword ? 'text' : 'password'}
                className="wh-input"
                autoComplete="current-password"
                maxLength={PASSWORD_MAX}
                value={password}
                aria-invalid={!!errors.password}
                aria-describedby={errors.password ? 'password-error' : undefined}
                disabled={submitting}
                onChange={(e: ChangeEvent<HTMLInputElement>) => setPassword(e.target.value)}
              />
              <button
                type="button"
                className="password-toggle"
                aria-label={showPassword ? 'Hide password' : 'Show password'}
                aria-pressed={showPassword}
                onClick={() => setShowPassword((visible) => !visible)}
              >
                <i className={showPassword ? 'pi pi-eye-slash' : 'pi pi-eye'} aria-hidden="true" />
              </button>
            </div>
            {errors.password && (
              <small id="password-error" className="field-error">
                {errors.password}
              </small>
            )}
          </div>

          <label className="checkbox-row" htmlFor="remember">
            <input
              id="remember"
              name="remember"
              type="checkbox"
              checked={rememberMe}
              disabled={submitting}
              onChange={(e: ChangeEvent<HTMLInputElement>) => setRememberMe(e.target.checked)}
            />
            <span>Keep me signed in</span>
          </label>

          <button type="submit" className="wh-btn" disabled={submitting} aria-busy={submitting}>
            {submitting && <i className="pi pi-spin pi-spinner" aria-hidden="true" />}
            <span>{submitting ? 'Signing in…' : 'Sign in'}</span>
          </button>
        </form>
      </section>
    </main>
  )
}
