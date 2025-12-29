import axios from 'axios'
import type { Pinia } from 'pinia'
import type { Router } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useNotifyStore } from '../stores/notify'
import type { ProblemDetails } from '../types/problemDetails'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'

export const api = axios.create({
  baseURL,
})

export function setupAxiosInterceptors(pinia: Pinia, router: Router) {
  api.interceptors.request.use((config) => {
    const auth = useAuthStore(pinia)
    if (auth.token) {
      config.headers = config.headers ?? {}
      config.headers.Authorization = `Bearer ${auth.token}`
    }
    if (auth.isAdmin && auth.isActingAsCompany && auth.actingCompanyUserId) {
      config.headers = config.headers ?? {}
      config.headers['X-Act-As-CompanyUserId'] = auth.actingCompanyUserId
    }
    if (auth.isAdmin && auth.isActingAsExpert && auth.actingExpertUserId) {
      config.headers = config.headers ?? {}
      config.headers['X-Act-As-ExpertUserId'] = auth.actingExpertUserId
    }
    return config
  })

  api.interceptors.response.use(
    (response) => response,
    async (error) => {
      const notify = useNotifyStore(pinia)
      const auth = useAuthStore(pinia)

      const status = error?.response?.status as number | undefined
      const problem = error?.response?.data as ProblemDetails | undefined

      if (status === 401) {
        auth.logout()
        await router.push('/login')
        notify.notify('Session expired. Please sign in again.', 'warning')
        return Promise.reject(error)
      }

      let message = error?.message ?? 'Request failed.'
      if (problem) {
        if (problem.errors) {
          const firstKey = Object.keys(problem.errors)[0]
          const firstError = firstKey ? problem.errors[firstKey]?.[0] : undefined
          if (firstError) {
            message = firstError
          }
        } else if (problem.title || problem.detail) {
          const parts = [problem.title, problem.detail].filter(Boolean)
          message = parts.join(' - ')
        }
      }

      notify.notify(message, 'error')
      return Promise.reject(error)
    }
  )
}
