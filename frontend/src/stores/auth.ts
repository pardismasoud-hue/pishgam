import { defineStore } from 'pinia'
import { api } from '../plugins/axios'

export type UserRole = 'Admin' | 'Expert' | 'Company'

interface AuthState {
  token: string | null
  role: UserRole | null
  email: string | null
  actingRole: UserRole | null
  actingCompanyUserId: string | null
  actingCompanyLabel: string | null
  actingExpertUserId: string | null
  actingExpertLabel: string | null
}

const TOKEN_KEY = 'msp_token'
const ROLE_KEY = 'msp_role'
const EMAIL_KEY = 'msp_email'
const ACTING_ROLE_KEY = 'msp_acting_role'
const ACTING_COMPANY_ID_KEY = 'msp_acting_company_user_id'
const ACTING_COMPANY_LABEL_KEY = 'msp_acting_company_label'
const ACTING_EXPERT_ID_KEY = 'msp_acting_expert_user_id'
const ACTING_EXPERT_LABEL_KEY = 'msp_acting_expert_label'

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: localStorage.getItem(TOKEN_KEY),
    role: (localStorage.getItem(ROLE_KEY) as UserRole | null) ?? null,
    email: localStorage.getItem(EMAIL_KEY),
    actingRole: (localStorage.getItem(ACTING_ROLE_KEY) as UserRole | null) ?? null,
    actingCompanyUserId: localStorage.getItem(ACTING_COMPANY_ID_KEY),
    actingCompanyLabel: localStorage.getItem(ACTING_COMPANY_LABEL_KEY),
    actingExpertUserId: localStorage.getItem(ACTING_EXPERT_ID_KEY),
    actingExpertLabel: localStorage.getItem(ACTING_EXPERT_LABEL_KEY),
  }),
  getters: {
    isAuthenticated: (state) => !!state.token,
    isAdmin: (state) => state.role === 'Admin',
    isExpert: (state) => state.role === 'Expert',
    isCompany: (state) => state.role === 'Company',
    isActingAsCompany: (state) => state.actingRole === 'Company' && !!state.actingCompanyUserId,
    isActingAsExpert: (state) => state.actingRole === 'Expert' && !!state.actingExpertUserId,
  },
  actions: {
    setAuth(token: string, role: UserRole, email?: string | null) {
      this.token = token
      this.role = role
      this.email = email ?? null
      localStorage.setItem(TOKEN_KEY, token)
      localStorage.setItem(ROLE_KEY, role)
      if (email) {
        localStorage.setItem(EMAIL_KEY, email)
      }
    },
    clearAuth() {
      this.token = null
      this.role = null
      this.email = null
      localStorage.removeItem(TOKEN_KEY)
      localStorage.removeItem(ROLE_KEY)
      localStorage.removeItem(EMAIL_KEY)
      this.clearActingContext()
    },
    setActingCompany(userId: string, label: string) {
      this.actingRole = 'Company'
      this.actingCompanyUserId = userId
      this.actingCompanyLabel = label
      localStorage.setItem(ACTING_ROLE_KEY, this.actingRole)
      localStorage.setItem(ACTING_COMPANY_ID_KEY, userId)
      localStorage.setItem(ACTING_COMPANY_LABEL_KEY, label)
      this.actingExpertUserId = null
      this.actingExpertLabel = null
      localStorage.removeItem(ACTING_EXPERT_ID_KEY)
      localStorage.removeItem(ACTING_EXPERT_LABEL_KEY)
    },
    setActingExpert(userId: string, label: string) {
      this.actingRole = 'Expert'
      this.actingExpertUserId = userId
      this.actingExpertLabel = label
      localStorage.setItem(ACTING_ROLE_KEY, this.actingRole)
      localStorage.setItem(ACTING_EXPERT_ID_KEY, userId)
      localStorage.setItem(ACTING_EXPERT_LABEL_KEY, label)
      this.actingCompanyUserId = null
      this.actingCompanyLabel = null
      localStorage.removeItem(ACTING_COMPANY_ID_KEY)
      localStorage.removeItem(ACTING_COMPANY_LABEL_KEY)
    },
    clearActingContext() {
      this.actingRole = null
      this.actingCompanyUserId = null
      this.actingCompanyLabel = null
      this.actingExpertUserId = null
      this.actingExpertLabel = null
      localStorage.removeItem(ACTING_ROLE_KEY)
      localStorage.removeItem(ACTING_COMPANY_ID_KEY)
      localStorage.removeItem(ACTING_COMPANY_LABEL_KEY)
      localStorage.removeItem(ACTING_EXPERT_ID_KEY)
      localStorage.removeItem(ACTING_EXPERT_LABEL_KEY)
    },
    async login(email: string, password: string) {
      const response = await api.post('/auth/login', { email, password })
      const token = response.data?.accessToken as string | undefined
      const role = response.data?.role as UserRole | undefined

      if (!token || !role) {
        throw new Error('Invalid login response.')
      }

      this.setAuth(token, role, response.data?.email ?? email)
      return role
    },
    logout() {
      this.clearAuth()
    },
  },
})
