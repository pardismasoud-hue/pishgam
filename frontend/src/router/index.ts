import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import AdminLayout from '../layouts/AdminLayout.vue'
import ExpertLayout from '../layouts/ExpertLayout.vue'
import CompanyLayout from '../layouts/CompanyLayout.vue'
import PublicLayout from '../layouts/PublicLayout.vue'
import LoginPage from '../pages/auth/LoginPage.vue'
import ExpertRegisterPage from '../pages/auth/ExpertRegisterPage.vue'
import CompanyRegisterPage from '../pages/auth/CompanyRegisterPage.vue'
import AdminDashboardPage from '../pages/admin/AdminDashboardPage.vue'
import AdminExpertsPage from '../pages/admin/AdminExpertsPage.vue'
import AdminSkillsPage from '../pages/admin/AdminSkillsPage.vue'
import AdminServicesPage from '../pages/admin/AdminServicesPage.vue'
import AdminCompanyAssetsPage from '../pages/admin/AdminCompanyAssetsPage.vue'
import AdminContractsPage from '../pages/admin/AdminContractsPage.vue'
import AdminTicketsPage from '../pages/admin/AdminTicketsPage.vue'
import AdminReportsPage from '../pages/admin/AdminReportsPage.vue'
import AdminUsersPage from '../pages/admin/AdminUsersPage.vue'
import ExpertDashboardPage from '../pages/expert/ExpertDashboardPage.vue'
import ExpertAssetsPage from '../pages/expert/ExpertAssetsPage.vue'
import ExpertTicketsPage from '../pages/expert/ExpertTicketsPage.vue'
import ExpertKpiPage from '../pages/expert/ExpertKpiPage.vue'
import ExpertTimeLogsPage from '../pages/expert/ExpertTimeLogsPage.vue'
import CompanyDashboardPage from '../pages/company/CompanyDashboardPage.vue'
import CompanyAssetsPage from '../pages/company/CompanyAssetsPage.vue'
import CompanyContractsPage from '../pages/company/CompanyContractsPage.vue'
import CompanyTicketsPage from '../pages/company/CompanyTicketsPage.vue'
import CompanySatisfactionPage from '../pages/company/CompanySatisfactionPage.vue'
import PlaceholderPage from '../pages/placeholders/PlaceholderPage.vue'
import UnauthorizedPage from '../pages/shared/UnauthorizedPage.vue'
import NotFoundPage from '../pages/shared/NotFoundPage.vue'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/login',
  },
  {
    path: '/',
    component: PublicLayout,
    children: [
      { path: 'login', component: LoginPage },
      { path: 'register/expert', component: ExpertRegisterPage },
      { path: 'register/company', component: CompanyRegisterPage },
    ],
  },
  {
    path: '/admin',
    component: AdminLayout,
    meta: { requiresAuth: true, roles: ['Admin'] },
    children: [
      { path: '', component: AdminDashboardPage },
      { path: 'users', component: AdminUsersPage },
      { path: 'experts', component: AdminExpertsPage },
      { path: 'skills', component: AdminSkillsPage },
      { path: 'services', component: AdminServicesPage },
      { path: 'assets', component: AdminCompanyAssetsPage },
      { path: 'contracts', component: AdminContractsPage },
      { path: 'tickets', component: AdminTicketsPage },
      { path: 'reports', component: AdminReportsPage },
    ],
  },
  {
    path: '/expert',
    component: ExpertLayout,
    meta: { requiresAuth: true, roles: ['Expert'] },
    children: [
      { path: '', component: ExpertDashboardPage },
      { path: 'assets', component: ExpertAssetsPage },
      { path: 'tickets', component: ExpertTicketsPage },
      { path: 'timelogs', component: ExpertTimeLogsPage },
      { path: 'kpi', component: ExpertKpiPage },
    ],
  },
  {
    path: '/company',
    component: CompanyLayout,
    meta: { requiresAuth: true, roles: ['Company'] },
    children: [
      { path: '', component: CompanyDashboardPage },
      { path: 'assets', component: CompanyAssetsPage },
      { path: 'contracts', component: CompanyContractsPage },
      { path: 'tickets', component: CompanyTicketsPage },
      { path: 'satisfaction', component: CompanySatisfactionPage },
    ],
  },
  {
    path: '/unauthorized',
    component: UnauthorizedPage,
  },
  {
    path: '/:pathMatch(.*)*',
    component: NotFoundPage,
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach((to) => {
  const auth = useAuthStore()

  if (to.path === '/login' && auth.isAuthenticated && auth.role) {
    return { path: `/${auth.role.toLowerCase()}` }
  }

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { path: '/login', query: { redirect: to.fullPath } }
  }

  const allowedRoles = to.meta.roles as string[] | undefined
  if (allowedRoles && auth.role && !allowedRoles.includes(auth.role)) {
    if (auth.role === 'Admin') {
      const canActAsCompany = allowedRoles.includes('Company') && auth.isActingAsCompany
      const canActAsExpert = allowedRoles.includes('Expert') && auth.isActingAsExpert
      if (!canActAsCompany && !canActAsExpert) {
        return { path: '/unauthorized' }
      }
    } else {
      return { path: '/unauthorized' }
    }
  }

  return true
})

export default router
