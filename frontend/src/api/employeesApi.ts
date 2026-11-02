import { api } from './http'

export type LoginStatus = 'Active' | 'Inactive'

export interface Employee {
  id: string
  name: string
  email: string
  phone: string | null
  startDate: string | null // yyyy-MM-dd from the API. Shown as dd/MM/yyyy.
  profilePhoto: string | null
  hasAccess: boolean // true = has a login account
  role: string | null // role and status belong to the login, so they are null without access
  status: LoginStatus | null
  createdAtUtc: string
  lastLoginAtUtc: string | null
}

export interface EmployeeInput {
  name: string
  email: string
  phone: string | null
  startDate: string | null
  profilePhoto: string | null
  hasAccess: boolean
  role: string | null
  status: LoginStatus | null
  password: string | null
}

export const employeesApi = {
  list: async (search?: string): Promise<Employee[]> =>
    (await api.get<Employee[]>('/employees', { params: search ? { search } : undefined })).data,
  create: async (input: EmployeeInput): Promise<Employee> => (await api.post<Employee>('/employees', input)).data,
  update: async (id: string, input: EmployeeInput): Promise<Employee> =>
    (await api.put<Employee>(`/employees/${id}`, input)).data,
  remove: async (id: string): Promise<Employee> => (await api.delete<Employee>(`/employees/${id}`)).data,
}
