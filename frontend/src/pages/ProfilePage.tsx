import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { profileApi, type Profile } from '../api/profileApi'
import AppBar from '../components/AppBar'
import Avatar from '../components/Avatar'
import { logout } from '../auth/authService'
import { roleLabel } from '../auth/roles'
import { sessionStore, useSession } from '../auth/sessionStore'
import { readApiErrors } from '../utils/apiErrors'
import { resizePhoto } from '../utils/photo'

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const PHONE_PATTERN = /^(?:\+61\s?4|04)\d{2}\s?\d{3}\s?\d{3}$/

type Errors = Record<string, string>

// Updates the shared session so the top-right name and photo change straight away.
function applyToSession(profile: Profile) {
  sessionStore.updateUser({
    id: profile.id,
    name: profile.name,
    email: profile.email,
    isSuperAdmin: profile.isSuperAdmin,
    role: profile.role,
    profilePhoto: profile.profilePhoto,
  })
}

export default function ProfilePage() {
  const { user } = useSession()
  const navigate = useNavigate()
  const fileRef = useRef<HTMLInputElement>(null)

  const [profile, setProfile] = useState<Profile | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  // Details form
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [detailErrors, setDetailErrors] = useState<Errors>({})
  const [detailMessage, setDetailMessage] = useState<{ ok: boolean; text: string } | null>(null)
  const [savingDetails, setSavingDetails] = useState(false)

  // Photo
  const [photoError, setPhotoError] = useState<string | null>(null)
  const [savingPhoto, setSavingPhoto] = useState(false)

  // Password form
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [passwordErrors, setPasswordErrors] = useState<Errors>({})
  const [passwordMessage, setPasswordMessage] = useState<{ ok: boolean; text: string } | null>(null)
  const [savingPassword, setSavingPassword] = useState(false)

  function fill(next: Profile) {
    setProfile(next)
    setName(next.name)
    setEmail(next.email)
    setPhone(next.phone ?? '')
  }

  useEffect(() => {
    let cancelled = false
    profileApi
      .get()
      .then((loaded) => {
        if (!cancelled) fill(loaded)
      })
      .catch((error: unknown) => {
        if (!cancelled) setLoadError(readApiErrors(error).message ?? 'Could not load your profile.')
      })
    return () => {
      cancelled = true
    }
  }, [])

  async function savePhoto(photo: string | null) {
    setSavingPhoto(true)
    setPhotoError(null)
    try {
      const next = await profileApi.setPhoto(photo)
      setProfile(next)
      applyToSession(next)
    } catch (error) {
      setPhotoError(readApiErrors(error).message ?? 'Could not save the photo.')
    } finally {
      setSavingPhoto(false)
    }
  }

  async function handleFile(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    try {
      await savePhoto(await resizePhoto(file))
    } catch (e) {
      setPhotoError(e instanceof Error ? e.message : 'Could not read this image.')
    }
  }

  async function handleDetails(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (savingDetails || !profile) return

    // Quick checks for a better experience. The API checks everything again.
    const found: Errors = {}
    if (name.trim().length < 2 || name.trim().length > 100) found.name = 'Full name must be 2 to 100 characters.'
    if (!EMAIL_PATTERN.test(email.trim())) found.email = 'Enter a valid email address.'
    if (profile.hasEmployeeRecord && phone.trim() && !PHONE_PATTERN.test(phone.trim())) {
      found.phone = 'Use an Australian mobile, for example 0412 345 678 or +61 412 345 678.'
    }
    setDetailErrors(found)
    setDetailMessage(null)
    if (Object.keys(found).length > 0) return

    setSavingDetails(true)
    try {
      const next = await profileApi.update({
        name: name.trim(),
        email: email.trim(),
        phone: profile.hasEmployeeRecord ? phone.trim() || null : null,
      })
      fill(next)
      applyToSession(next)
      setDetailMessage({ ok: true, text: 'Your details were saved.' })
    } catch (error) {
      const result = readApiErrors(error)
      setDetailErrors(result.fields)
      setDetailMessage({ ok: false, text: result.message ?? 'Please fix the highlighted fields.' })
    } finally {
      setSavingDetails(false)
    }
  }

  async function handlePassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (savingPassword) return

    const found: Errors = {}
    if (!currentPassword) found.currentPassword = 'Enter your current password.'
    if (newPassword.length < 12) found.newPassword = 'New password must be at least 12 characters.'
    setPasswordErrors(found)
    setPasswordMessage(null)
    if (Object.keys(found).length > 0) return

    setSavingPassword(true)
    try {
      await profileApi.changePassword(currentPassword, newPassword)
      setPasswordMessage({ ok: true, text: 'Password changed. Please sign in again with your new password.' })
      // The API ended all sessions, so sign out here too.
      setTimeout(() => {
        void logout().then(() => navigate('/login', { replace: true }))
      }, 1500)
    } catch (error) {
      const result = readApiErrors(error)
      setPasswordErrors(result.fields)
      setPasswordMessage({ ok: false, text: result.message ?? 'Please fix the highlighted fields.' })
      setSavingPassword(false)
    }
  }

  const err = (errors: Errors, key: string) =>
    errors[key] && (
      <small id={`${key}-error`} className="field-error">
        {errors[key]}
      </small>
    )

  const invalid = (errors: Errors, key: string) => ({
    'aria-invalid': !!errors[key],
    'aria-describedby': errors[key] ? `${key}-error` : undefined,
  })

  return (
    <>
      <AppBar />

      <main className="page">
        <h1>Profile</h1>

        {loadError && (
          <div className="form-message" role="alert">
            {loadError}
          </div>
        )}

        {!profile && !loadError && (
          <div className="panel" role="status">
            <p>Loading…</p>
          </div>
        )}

        {profile && (
          <div className="profile-stack">
            <section className="panel profile-panel" aria-labelledby="photo-heading">
              <h2 id="photo-heading">Photo</h2>
              {photoError && (
                <div className="form-message" role="alert">
                  {photoError}
                </div>
              )}
              <div className="photo-row">
                <Avatar name={user?.name ?? ''} photo={user?.profilePhoto} size="lg" />
                <div className="photo-actions">
                  <input ref={fileRef} type="file" accept="image/jpeg,image/png,image/webp" hidden onChange={handleFile} />
                  <button type="button" className="wh-btn secondary" disabled={savingPhoto} onClick={() => fileRef.current?.click()}>
                    <i className="pi pi-camera" aria-hidden="true" />
                    <span>{savingPhoto ? 'Saving…' : user?.profilePhoto ? 'Change photo' : 'Add photo'}</span>
                  </button>
                  {user?.profilePhoto && (
                    <button type="button" className="wh-btn text" disabled={savingPhoto} onClick={() => void savePhoto(null)}>
                      Remove
                    </button>
                  )}
                </div>
              </div>
            </section>

            <section className="panel profile-panel" aria-labelledby="details-heading">
              <h2 id="details-heading">Your details</h2>
              <form onSubmit={handleDetails} noValidate>
                {detailMessage && (
                  <div className={detailMessage.ok ? 'form-message ok' : 'form-message'} role={detailMessage.ok ? 'status' : 'alert'}>
                    {detailMessage.text}
                  </div>
                )}

                <div className="field">
                  <label htmlFor="name">Full name</label>
                  <input id="name" className="wh-input" maxLength={100} autoComplete="name" value={name} disabled={savingDetails}
                    {...invalid(detailErrors, 'name')} onChange={(e) => setName(e.target.value)} />
                  {err(detailErrors, 'name')}
                </div>

                <div className="field">
                  <label htmlFor="email">Email (used to log in)</label>
                  <input id="email" className="wh-input" type="email" inputMode="email" autoCapitalize="none" spellCheck={false}
                    maxLength={254} autoComplete="email" value={email} disabled={savingDetails}
                    {...invalid(detailErrors, 'email')} onChange={(e) => setEmail(e.target.value)} />
                  {err(detailErrors, 'email')}
                </div>

                {profile.hasEmployeeRecord && (
                  <div className="field">
                    <label htmlFor="phone">Phone</label>
                    <input id="phone" className="wh-input" type="tel" inputMode="tel" maxLength={20} autoComplete="tel"
                      placeholder="04xx xxx xxx" value={phone} disabled={savingDetails}
                      {...invalid(detailErrors, 'phone')} onChange={(e) => setPhone(e.target.value)} />
                    {err(detailErrors, 'phone')}
                  </div>
                )}

                <dl className="card-details">
                  <div>
                    <dt>Role</dt>
                    <dd>{roleLabel(profile.role)}</dd>
                  </div>
                </dl>
                <small className="field-hint">Only a Super Admin can change your role or deactivate your account.</small>

                <button type="submit" className="wh-btn save-btn" disabled={savingDetails} aria-busy={savingDetails}>
                  {savingDetails && <i className="pi pi-spin pi-spinner" aria-hidden="true" />}
                  <span>{savingDetails ? 'Saving…' : 'Save changes'}</span>
                </button>
              </form>
            </section>

            <section className="panel profile-panel" aria-labelledby="password-heading">
              <h2 id="password-heading">Change password</h2>
              <form onSubmit={handlePassword} noValidate>
                {passwordMessage && (
                  <div className={passwordMessage.ok ? 'form-message ok' : 'form-message'} role={passwordMessage.ok ? 'status' : 'alert'}>
                    {passwordMessage.text}
                  </div>
                )}

                <div className="field">
                  <label htmlFor="currentPassword">Current password</label>
                  <input id="currentPassword" className="wh-input" type="password" maxLength={128} autoComplete="current-password"
                    value={currentPassword} disabled={savingPassword}
                    {...invalid(passwordErrors, 'currentPassword')} onChange={(e) => setCurrentPassword(e.target.value)} />
                  {err(passwordErrors, 'currentPassword')}
                </div>

                <div className="field">
                  <label htmlFor="newPassword">New password</label>
                  <input id="newPassword" className="wh-input" type="password" maxLength={128} autoComplete="new-password"
                    value={newPassword} disabled={savingPassword}
                    {...invalid(passwordErrors, 'newPassword')} onChange={(e) => setNewPassword(e.target.value)} />
                  {err(passwordErrors, 'newPassword')}
                  {!passwordErrors.newPassword && (
                    <small className="field-hint">At least 12 characters with upper and lower case, a number and a symbol.</small>
                  )}
                </div>

                <button type="submit" className="wh-btn save-btn" disabled={savingPassword} aria-busy={savingPassword}>
                  {savingPassword && <i className="pi pi-spin pi-spinner" aria-hidden="true" />}
                  <span>{savingPassword ? 'Saving…' : 'Change password'}</span>
                </button>
              </form>
            </section>
          </div>
        )}
      </main>
    </>
  )
}
