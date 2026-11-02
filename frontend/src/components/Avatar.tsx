interface AvatarProps {
  name: string
  photo: string | null | undefined
  size?: 'sm' | 'md' | 'lg'
}

// Profile photo, or a person icon when there is no photo.
export default function Avatar({ name, photo, size = 'md' }: AvatarProps) {
  return (
    <span className={`avatar avatar-${size}`}>
      {photo ? <img src={photo} alt={name ? `Photo of ${name}` : ''} /> : <i className="pi pi-user" aria-hidden="true" />}
    </span>
  )
}
