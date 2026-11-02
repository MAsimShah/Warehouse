import { useEffect, useRef, useState, type ChangeEvent, type FormEvent, type ReactNode } from 'react'
import { employeesApi, type Employee, type LoginStatus } from '../../api/employeesApi'
import Avatar from '../../components/Avatar'
import { ASSIGNABLE_ROLES, roleLabel } from '../../auth/roles'
import { resizePhoto } from '../../utils/photo'
import {
  EMPTY_FORM,
  apiErrors,
  toFormValues,
  toInput,
  validate,
  type EmployeeFormValues,
  type FormErrors,
} from './employeeForm'

interface EmployeeDialogProps {
  employee: Employee | null // null = add a new employee
  onClose: () => void
  onSaved: () => void
}

export default function EmployeeDialog({ employee, onClose, onSaved }: EmployeeDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const fileRef = useRef<HTMLInputElement>(null)
  const isNew = employee === null
  const hadLogin = employee?.hasAccess ?? false

  const [values, setValues] = useState<EmployeeFormValues>(employee ? toFormValues(employee) : EMPTY_FORM)
  const [errors, setErrors] = useState<FormErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [showPassword, setShowPassword] = useState(false)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    const dialog = dialogRef.current
    if (dialog && !dialog.open) dialog.showModal()
  }, [])

  function set<K extends keyof EmployeeFormValues>(key: K, value: EmployeeFormValues[K]) {
    setValues((current) => ({ ...current, [key]: value }))
  }

  async function handlePhoto(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    try {
      set('profilePhoto', await resizePhoto(file))
      setErrors((current) => ({ ...current, profilePhoto: undefined }))
    } catch (error) {
      setErrors((current) => ({
        ...current,
        profilePhoto: error instanceof Error ? error.message : 'Could not read this image.',
      }))
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (saving) return

    const found = validate(values, hadLogin)
    setErrors(found)
    setFormError(null)
    if (Object.keys(found).length > 0) return

    setSaving(true)
    try {
      const input = toInput(values)
      if (isNew) await employeesApi.create(input)
      else await employeesApi.update(employee.id, input)
      onSaved()
    } catch (error) {
      const result = apiErrors(error)
      setErrors(result.fields)
      setFormError(result.message ?? (Object.keys(result.fields).length ? 'Please fix the highlighted fields.' : null))
    } finally {
      setSaving(false)
    }
  }

  function field(key: keyof EmployeeFormValues, label: string, control: ReactNode, hint?: string) {
    return (
      <div className="field">
        <label htmlFor={`emp-${key}`}>{label}</label>
        {control}
        {hint && !errors[key] && <small className="field-hint">{hint}</small>}
        {errors[key] && (
          <small id={`emp-${key}-error`} className="field-error">
            {errors[key]}
          </small>
        )}
      </div>
    )
  }

  const inputProps = (key: keyof EmployeeFormValues) => ({
    id: `emp-${key}`,
    className: 'wh-input',
    disabled: saving,
    'aria-invalid': !!errors[key],
    'aria-describedby': errors[key] ? `emp-${key}-error` : undefined,
  })

  return (
    <dialog ref={dialogRef} className="dialog" aria-labelledby="employee-dialog-title" onClose={onClose} onCancel={saving ? (e) => e.preventDefault() : undefined}>
      <form onSubmit={handleSubmit} noValidate>
        <header className="dialog-header">
          <h2 id="employee-dialog-title">{isNew ? 'Add employee' : 'Edit employee'}</h2>
          <button type="button" className="icon-btn" aria-label="Close" disabled={saving} onClick={() => dialogRef.current?.close()}>
            <i className="pi pi-times" aria-hidden="true" />
          </button>
        </header>

        <div className="dialog-body">
          {formError && (
            <div className="form-message" role="alert">
              {formError}
            </div>
          )}

          <div className="photo-row">
            <Avatar name={values.name} photo={values.profilePhoto} size="lg" />
            <div className="photo-actions">
              <input ref={fileRef} type="file" accept="image/jpeg,image/png,image/webp" hidden onChange={handlePhoto} />
              <button type="button" className="wh-btn secondary" disabled={saving} onClick={() => fileRef.current?.click()}>
                <i className="pi pi-camera" aria-hidden="true" />
                <span>{values.profilePhoto ? 'Change photo' : 'Add photo'}</span>
              </button>
              {values.profilePhoto && (
                <button type="button" className="wh-btn text" disabled={saving} onClick={() => set('profilePhoto', null)}>
                  Remove
                </button>
              )}
            </div>
            {errors.profilePhoto && <small className="field-error">{errors.profilePhoto}</small>}
          </div>

          {field('name', 'Full name', (
            <input {...inputProps('name')} value={values.name} maxLength={100} autoComplete="off" onChange={(e) => set('name', e.target.value)} />
          ))}

          {field('email', 'Email', (
            <input
              {...inputProps('email')}
              type="email"
              inputMode="email"
              autoCapitalize="none"
              spellCheck={false}
              maxLength={254}
              autoComplete="off"
              value={values.email}
              onChange={(e) => set('email', e.target.value)}
            />
          ), values.hasAccess ? 'This is also the login email.' : undefined)}

          {field('phone', 'Phone', (
            <input
              {...inputProps('phone')}
              type="tel"
              inputMode="tel"
              maxLength={20}
              autoComplete="off"
              placeholder="04xx xxx xxx"
              value={values.phone}
              onChange={(e) => set('phone', e.target.value)}
            />
          ))}

          {field('startDate', 'Start date', (
            <input
              {...inputProps('startDate')}
              inputMode="numeric"
              placeholder="dd/MM/yyyy"
              maxLength={10}
              autoComplete="off"
              value={values.startDate}
              onChange={(e) => set('startDate', e.target.value)}
            />
          ))}

          <fieldset className="access-box">
            <legend className="sr-only">System access</legend>
            <label className="checkbox-row access-toggle" htmlFor="emp-hasAccess">
              <input
                id="emp-hasAccess"
                type="checkbox"
                checked={values.hasAccess}
                disabled={saving}
                onChange={(e) => set('hasAccess', e.target.checked)}
              />
              <span>Give system access (can log in)</span>
            </label>
            <small className="field-hint access-hint">
              Leave this off to keep only an employee record, with no login.
            </small>

            {values.hasAccess && (
              <>
                <div className="field-row">
                  {field('role', 'Role', (
                    <select {...inputProps('role')} value={values.role} onChange={(e) => set('role', e.target.value)}>
                      <option value="">Select a role</option>
                      {ASSIGNABLE_ROLES.map((role) => (
                        <option key={role} value={role}>
                          {roleLabel(role)}
                        </option>
                      ))}
                    </select>
                  ))}

                  {field('status', 'Status', (
                    <select {...inputProps('status')} value={values.status} onChange={(e) => set('status', e.target.value as LoginStatus)}>
                      <option value="Active">Active</option>
                      <option value="Inactive">Inactive</option>
                    </select>
                  ))}
                </div>

                {field('password', hadLogin ? 'New password' : 'Password', (
                  <div className="password-wrap">
                    <input
                      {...inputProps('password')}
                      type={showPassword ? 'text' : 'password'}
                      maxLength={128}
                      autoComplete="new-password"
                      value={values.password}
                      onChange={(e) => set('password', e.target.value)}
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
                ), hadLogin ? 'Leave empty to keep the current password.' : 'At least 12 characters with upper and lower case, a number and a symbol.')}
              </>
            )}
          </fieldset>
        </div>

        <footer className="dialog-footer">
          <button type="button" className="wh-btn secondary" disabled={saving} onClick={() => dialogRef.current?.close()}>
            Cancel
          </button>
          <button type="submit" className="wh-btn" disabled={saving} aria-busy={saving}>
            {saving && <i className="pi pi-spin pi-spinner" aria-hidden="true" />}
            <span>{saving ? 'Saving…' : 'Save'}</span>
          </button>
        </footer>
      </form>
    </dialog>
  )
}
