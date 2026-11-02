import { useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import UserMenu from './UserMenu'
import { logout } from '../auth/authService'
import { canManageEmployees, roleLabel } from '../auth/roles'
import { useSession } from '../auth/sessionStore'

// Top bar shared by all signed-in pages. User photo, name and role are on the top right.
export default function AppBar() {
  const { user } = useSession()
  const navigate = useNavigate()
  const [loggingOut, setLoggingOut] = useState(false)

  async function handleLogout() {
    setLoggingOut(true)
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <header className="app-bar">
      <nav className="app-nav" aria-label="Main">
        <NavLink to="/" end className="app-name">
          Warehouse
        </NavLink>
        {canManageEmployees(user) && (
          <NavLink to="/employees" className="nav-link">
            Employees
          </NavLink>
        )}
      </nav>
      <UserMenu
        name={user?.name ?? ''}
        role={roleLabel(user?.role ?? '')}
        photo={user?.profilePhoto ?? null}
        loggingOut={loggingOut}
        onLogout={handleLogout}
        onProfile={() => navigate('/profile')}
      />
    </header>
  )
}
