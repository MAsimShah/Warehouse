import { useCallback, useEffect, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { employeesApi, type Employee } from '../api/employeesApi'
import AppBar from '../components/AppBar'
import Avatar from '../components/Avatar'
import { canManageEmployees, roleLabel } from '../auth/roles'
import { useSession } from '../auth/sessionStore'
import EmployeeDialog from './employees/EmployeeDialog'
import { apiErrors, isoToDisplay } from './employees/employeeForm'

export default function EmployeesPage() {
  const { user } = useSession()

  const [employees, setEmployees] = useState<Employee[]>([])
  const [search, setSearch] = useState('')
  const [query, setQuery] = useState('')
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [editing, setEditing] = useState<Employee | null | undefined>(undefined) // undefined = dialog closed
  const [actionError, setActionError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  // Wait a moment after typing before asking the server.
  useEffect(() => {
    const timer = setTimeout(() => setQuery(search.trim()), 300)
    return () => clearTimeout(timer)
  }, [search])

  const load = useCallback(async () => {
    setLoading(true)
    setLoadError(null)
    try {
      setEmployees(await employeesApi.list(query || undefined))
    } catch (error) {
      setLoadError(apiErrors(error).message ?? 'Could not load employees.')
    } finally {
      setLoading(false)
    }
  }, [query])

  useEffect(() => {
    void load()
  }, [load])

  if (!canManageEmployees(user)) return <Navigate to="/" replace />

  // With a login: the login is deactivated and the record stays. Record only: the record is deleted.
  async function handleRemove(employee: Employee) {
    const question = employee.hasAccess
      ? `Deactivate ${employee.name}? They will not be able to log in. Their record is kept.`
      : `Delete ${employee.name}? This removes the employee record.`
    if (!window.confirm(question)) return
    setBusyId(employee.id)
    setActionError(null)
    try {
      await employeesApi.remove(employee.id)
      await load()
    } catch (error) {
      const result = apiErrors(error)
      setActionError(result.fields.status ?? result.message ?? 'Could not complete this action.')
    } finally {
      setBusyId(null)
    }
  }

  function statusBadge(employee: Employee) {
    if (!employee.hasAccess) return <span className="badge badge-inactive">No access</span>
    return <span className={`badge ${employee.status === 'Active' ? 'badge-active' : 'badge-inactive'}`}>{employee.status}</span>
  }

  const roleText = (employee: Employee) => (employee.role ? roleLabel(employee.role) : '—')

  function rowActions(employee: Employee) {
    return (
      <div className="row-actions">
        <button type="button" className="wh-btn secondary" onClick={() => setEditing(employee)} aria-label={`Edit ${employee.name}`}>
          <i className="pi pi-pencil" aria-hidden="true" />
          <span>Edit</span>
        </button>
        {(!employee.hasAccess || employee.status === 'Active') && (
          <button
            type="button"
            className="wh-btn text danger"
            disabled={busyId === employee.id}
            onClick={() => void handleRemove(employee)}
            aria-label={`${employee.hasAccess ? 'Deactivate' : 'Delete'} ${employee.name}`}
          >
            <i className={employee.hasAccess ? 'pi pi-ban' : 'pi pi-trash'} aria-hidden="true" />
            <span>{employee.hasAccess ? 'Deactivate' : 'Delete'}</span>
          </button>
        )}
      </div>
    )
  }

  return (
    <>
      <AppBar />

      <main className="page">
        <div className="page-head">
          <h1>Employees</h1>
          <button type="button" className="wh-btn add-btn" onClick={() => setEditing(null)}>
            <i className="pi pi-plus" aria-hidden="true" />
            <span>Add employee</span>
          </button>
        </div>

        <div className="field search-field">
          <label htmlFor="employee-search" className="sr-only">
            Search employees
          </label>
          <input
            id="employee-search"
            type="search"
            className="wh-input"
            placeholder="Search by name or email"
            maxLength={100}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        {actionError && (
          <div className="form-message" role="alert">
            {actionError}
          </div>
        )}
        {loadError && (
          <div className="form-message" role="alert">
            {loadError}
          </div>
        )}

        {loading && employees.length === 0 ? (
          <div className="panel" role="status">
            <p>Loading…</p>
          </div>
        ) : employees.length === 0 && !loadError ? (
          <div className="panel">
            <h2>{query ? 'No matching employees' : 'No employees yet'}</h2>
            {!query && <p>Use “Add employee” to create the first one.</p>}
          </div>
        ) : (
          <>
            {/* Desktop and tablet: table. */}
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th scope="col">Employee</th>
                    <th scope="col">Role</th>
                    <th scope="col">Phone</th>
                    <th scope="col">Start date</th>
                    <th scope="col">Status</th>
                    <th scope="col">
                      <span className="sr-only">Actions</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {employees.map((employee) => (
                    <tr key={employee.id}>
                      <td>
                        <div className="person">
                          <Avatar name={employee.name} photo={employee.profilePhoto} />
                          <div>
                            <div className="person-name">{employee.name}</div>
                            <div className="muted person-email">{employee.email}</div>
                          </div>
                        </div>
                      </td>
                      <td>{roleText(employee)}</td>
                      <td>{employee.phone ?? '—'}</td>
                      <td>{isoToDisplay(employee.startDate) || '—'}</td>
                      <td>{statusBadge(employee)}</td>
                      <td>{rowActions(employee)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Phones: cards. */}
            <ul className="card-list">
              {employees.map((employee) => (
                <li key={employee.id} className="card">
                  <div className="person">
                    <Avatar name={employee.name} photo={employee.profilePhoto} />
                    <div>
                      <div className="person-name">{employee.name}</div>
                      <div className="muted person-email">{employee.email}</div>
                    </div>
                    {statusBadge(employee)}
                  </div>
                  <dl className="card-details">
                    <div>
                      <dt>Role</dt>
                      <dd>{roleText(employee)}</dd>
                    </div>
                    <div>
                      <dt>Phone</dt>
                      <dd>{employee.phone ?? '—'}</dd>
                    </div>
                    <div>
                      <dt>Start date</dt>
                      <dd>{isoToDisplay(employee.startDate) || '—'}</dd>
                    </div>
                  </dl>
                  {rowActions(employee)}
                </li>
              ))}
            </ul>
          </>
        )}
      </main>

      {editing !== undefined && (
        <EmployeeDialog
          employee={editing}
          onClose={() => setEditing(undefined)}
          onSaved={() => {
            setEditing(undefined)
            void load()
          }}
        />
      )}
    </>
  )
}
