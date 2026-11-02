import type { AuthUser } from '../auth/types'
import { api } from './http'

// The signed-in user's own details. Role and status are shown but cannot be changed here.
export interface Profile extends AuthUser {
  phone: string | null
  hasEmployeeRecord: boolean // false for the Super Admin, so there is no phone
}

export interface UpdateProfileInput {
  name: string
  email: string
  phone: string | null
}

export const profileApi = {
  get: async (): Promise<Profile> => (await api.get<Profile>('/profile')).data,
  update: async (input: UpdateProfileInput): Promise<Profile> => (await api.put<Profile>('/profile', input)).data,
  // photo = null removes the photo.
  setPhoto: async (photo: string | null): Promise<Profile> =>
    (await api.put<Profile>('/profile/photo', { profilePhoto: photo })).data,
  changePassword: async (currentPassword: string, newPassword: string): Promise<void> => {
    await api.post('/profile/password', { currentPassword, newPassword })
  },
}
