import axios from 'axios'
import type { Employee, EmployeeInput, LoginStatus } from '../../api/employeesApi'

export interface EmployeeFormValues {
  name: string
  email: string
  phone: string
  startDate: string // dd/MM/yyyy as typed
  profilePhoto: string | null
  hasAccess: boolean
  role: string
  status: LoginStatus
  password: string
}

export type FormErrors = Partial<Record<keyof EmployeeFormValues, string>>

export const EMPTY_FORM: EmployeeFormValues = {
  name: '',
  email: '',
  phone: '',
  startDate: '',
  profilePhoto: null,
  hasAccess: false,
  role: '',
  status: 'Active',
  password: '',
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const PHONE_PATTERN = /^(?:\+61\s?4|04)\d{2}\s?\d{3}\s?\d{3}$/

export function isoToDisplay(iso: string | null): string {
  if (!iso) return ''
  const [y, m, d] = iso.split('-')
  return `${d}/${m}/${y}`
}

function displayToIso(value: string): string | null {
  const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(value.trim())
  if (!match) return null
  const [, d, m, y] = match
  const date = new Date(Number(y), Number(m) - 1, Number(d))
  const valid = date.getFullYear() === Number(y) && date.getMonth() === Number(m) - 1 && date.getDate() === Number(d)
  return valid ? `${y}-${m}-${d}` : null
}

export function toFormValues(employee: Employee): EmployeeFormValues {
  return {
    name: employee.name,
    email: employee.email,
    phone: employee.phone ?? '',
    startDate: isoToDisplay(employee.startDate),
    profilePhoto: employee.profilePhoto,
    // A login that was switched off earlier (Inactive) shows the access section off.
    hasAccess: employee.hasAccess && employee.status === 'Active',
    role: employee.role ?? '',
    status: employee.status ?? 'Active',
    password: '',
  }
}

// Quick checks for a better experience. The API checks everything again.
// hadLogin = the employee already has a login account, so a password is not needed again.
export function validate(values: EmployeeFormValues, hadLogin: boolean): FormErrors {
  const errors: FormErrors = {}
  const name = values.name.trim()

  if (name.length < 2 || name.length > 100) errors.name = 'Full name must be 2 to 100 characters.'
  if (!EMAIL_PATTERN.test(values.email.trim())) errors.email = 'Enter a valid email address.'
  if (values.phone.trim() && !PHONE_PATTERN.test(values.phone.trim())) {
    errors.phone = 'Use an Australian mobile, for example 0412 345 678 or +61 412 345 678.'
  }
  if (values.startDate.trim() && !displayToIso(values.startDate)) errors.startDate = 'Use dd/MM/yyyy, for example 25/11/2026.'

  if (values.hasAccess) {
    if (!values.role) errors.role = 'Choose a role.'
    if (!hadLogin && !values.password) errors.password = 'Password is required to give access.'
    else if (values.password && values.password.length < 12) errors.password = 'Password must be at least 12 characters.'
  }

  return errors
}

export function toInput(values: EmployeeFormValues): EmployeeInput {
  return {
    name: values.name.trim(),
    email: values.email.trim(),
    phone: values.phone.trim() || null,
    startDate: values.startDate.trim() ? displayToIso(values.startDate) : null,
    profilePhoto: values.profilePhoto,
    hasAccess: values.hasAccess,
    role: values.hasAccess ? values.role : null,
    status: values.hasAccess ? values.status : null,
    password: values.hasAccess ? values.password || null : null,
  }
}

// Field errors from the API (ProblemDetails "errors"), with the first letter lower-cased to match our field names.
export function apiErrors(error: unknown): { fields: FormErrors; message: string | null } {
  if (axios.isAxiosError(error)) {
    if (!error.response) return { fields: {}, message: 'Cannot reach the server. Check your connection and try again.' }

    const raw = (error.response.data as { errors?: Record<string, string[]> } | undefined)?.errors
    if (raw) {
      const fields: FormErrors = {}
      for (const [key, messages] of Object.entries(raw)) {
        const field = (key.charAt(0).toLowerCase() + key.slice(1)) as keyof EmployeeFormValues
        fields[field] = messages.join(' ')
      }
      return { fields, message: null }
    }
    if (error.response.status === 403) return { fields: {}, message: 'You do not have permission to do this.' }
    if (error.response.status === 404) return { fields: {}, message: 'This employee no longer exists.' }
    if (error.response.status === 413) return { fields: {}, message: 'The photo is too large.' }
  }
  return { fields: {}, message: 'Something went wrong. Please try again.' }
}
