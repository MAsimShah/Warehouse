export interface AuthUser {
  id: string
  name: string
  email: string
  isSuperAdmin: boolean
  role: string
  profilePhoto: string | null
}

// Response of POST /auth/login and POST /auth/refresh.
// The refresh token is NOT here: the API sets it as an HttpOnly cookie that JavaScript cannot read.
export interface AuthResponse {
  accessToken: string
  expiresAtUtc: string
  user: AuthUser
}
