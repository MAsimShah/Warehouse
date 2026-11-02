import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react'
import Avatar from './Avatar'

interface UserMenuProps {
  name: string
  role: string
  photo: string | null
  loggingOut: boolean
  onLogout: () => void
  onProfile: () => void
}

// Avatar button in the app bar. Opens a small menu with the user name, Profile and Sign out.
export default function UserMenu({ name, role, photo, loggingOut, onLogout, onProfile }: UserMenuProps) {
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const menuRef = useRef<HTMLDivElement>(null)
  const menuId = useId()

  function enabledItems(): HTMLElement[] {
    return Array.from(menuRef.current?.querySelectorAll<HTMLElement>('[role="menuitem"]:not([aria-disabled="true"])') ?? [])
  }

  // Close on click outside and on Escape.
  useEffect(() => {
    if (!open) return

    function handlePointerDown(event: PointerEvent) {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false)
    }

    function handleEscape(event: globalThis.KeyboardEvent) {
      if (event.key === 'Escape') {
        setOpen(false)
        triggerRef.current?.focus()
      }
    }

    document.addEventListener('pointerdown', handlePointerDown)
    document.addEventListener('keydown', handleEscape)
    return () => {
      document.removeEventListener('pointerdown', handlePointerDown)
      document.removeEventListener('keydown', handleEscape)
    }
  }, [open])

  // Move focus into the menu when it opens.
  useEffect(() => {
    if (open) enabledItems()[0]?.focus()
  }, [open])

  function handleMenuKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    const items = enabledItems()
    const index = items.indexOf(document.activeElement as HTMLElement)

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault()
        items[(index + 1) % items.length]?.focus()
        break
      case 'ArrowUp':
        event.preventDefault()
        items[(index - 1 + items.length) % items.length]?.focus()
        break
      case 'Home':
        event.preventDefault()
        items[0]?.focus()
        break
      case 'End':
        event.preventDefault()
        items[items.length - 1]?.focus()
        break
      case 'Tab':
        setOpen(false)
        break
    }
  }

  return (
    <div className="user-menu" ref={rootRef}>
      <button
        ref={triggerRef}
        type="button"
        className="avatar-btn"
        aria-label="Account menu"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-controls={open ? menuId : undefined}
        onClick={() => setOpen((value) => !value)}
      >
        <span className="avatar-btn-text">
          <span className="avatar-btn-name">{name}</span>
          <span className="avatar-btn-role">{role}</span>
        </span>
        <Avatar name={name} photo={photo} />
      </button>

      {open && (
        <div id={menuId} ref={menuRef} className="menu" role="menu" aria-label="Account" onKeyDown={handleMenuKeyDown}>
          <div className="menu-header">
            <span>{name}</span>
            <span className="menu-header-role">{role}</span>
          </div>

          <button
            type="button"
            role="menuitem"
            className="menu-item"
            onClick={() => {
              setOpen(false)
              onProfile()
            }}
          >
            <i className="pi pi-id-card" aria-hidden="true" />
            <span>Profile</span>
          </button>

          <button
            type="button"
            role="menuitem"
            className="menu-item"
            disabled={loggingOut}
            onClick={() => {
              setOpen(false)
              onLogout()
            }}
          >
            <i className="pi pi-sign-out" aria-hidden="true" />
            <span>{loggingOut ? 'Signing out…' : 'Sign out'}</span>
          </button>
        </div>
      )}
    </div>
  )
}
