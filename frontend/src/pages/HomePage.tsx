import AppBar from '../components/AppBar'
import { useSession } from '../auth/sessionStore'

export default function HomePage() {
  const { user } = useSession()

  return (
    <>
      <AppBar />

      <main className="page">
        <h1>Dashboard</h1>
        <section className="panel">
          <h2>Welcome, {user?.name}</h2>
          <p>
            Nothing to show yet. Parcels, shipments and trips will appear here as they are built.
          </p>
        </section>
      </main>
    </>
  )
}
