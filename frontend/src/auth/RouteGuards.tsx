import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useSession } from './sessionStore'

function FullPageSpinner() {
  return (
    <div className="page-center" role="status" aria-live="polite">
      <i className="pi pi-spin pi-spinner spinner" aria-hidden="true" />
      <span className="sr-only">Loading</span>
    </div>
  )
}

// Pages that need a logged-in user.
export function ProtectedRoute() {
  const { status } = useSession()
  const location = useLocation()

  if (status === 'loading') return <FullPageSpinner />
  if (status === 'anonymous') return <Navigate to="/login" replace state={{ from: location.pathname }} />
  return <Outlet />
}

// Pages for visitors only (login). Logged-in users go to the dashboard.
export function PublicOnlyRoute() {
  const { status } = useSession()

  if (status === 'loading') return <FullPageSpinner />
  if (status === 'authenticated') return <Navigate to="/" replace />
  return <Outlet />
}
