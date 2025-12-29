<template>
  <v-layout class="fill-height">
    <v-navigation-drawer permanent>
      <v-list density="compact" nav>
        <v-list-item title="MSP Admin" subtitle="Control Center" />
        <v-divider class="my-2" />
        <v-list-item v-for="link in adminLinks" :key="link.to" :to="link.to" :title="link.title" />
        <template v-if="auth.isActingAsCompany">
          <v-divider class="my-2" />
          <v-list-subheader>Company Portal</v-list-subheader>
          <v-list-item v-for="link in companyLinks" :key="link.to" :to="link.to" :title="link.title" />
        </template>
        <template v-if="auth.isActingAsExpert">
          <v-divider class="my-2" />
          <v-list-subheader>Expert Portal</v-list-subheader>
          <v-list-item v-for="link in expertLinks" :key="link.to" :to="link.to" :title="link.title" />
        </template>
      </v-list>
    </v-navigation-drawer>
    <v-main>
      <v-app-bar flat color="transparent">
        <v-spacer />
        <v-chip v-if="actingLabel" class="mr-2" color="secondary" variant="tonal">
          {{ actingLabel }}
        </v-chip>
        <v-menu v-model="actAsMenu" location="bottom end">
          <template #activator="{ props }">
            <v-btn v-bind="props" variant="tonal">Act As</v-btn>
          </template>
          <v-card min-width="360">
            <v-card-title class="text-body-1 font-weight-medium">Acting Context</v-card-title>
            <v-card-text>
              <v-select
                v-model="selectedCompanyId"
                :items="companyOptions"
                item-title="title"
                item-value="value"
                label="Company"
                clearable
                :loading="loadingCompanies"
              />
              <v-btn block color="primary" class="mb-4" @click="applyCompanyContext">
                Act as Company
              </v-btn>
              <v-select
                v-model="selectedExpertId"
                :items="expertOptions"
                item-title="title"
                item-value="value"
                label="Expert"
                clearable
                :loading="loadingExperts"
              />
              <v-btn block color="primary" @click="applyExpertContext">
                Act as Expert
              </v-btn>
            </v-card-text>
            <v-divider />
            <v-card-actions>
              <v-btn variant="text" @click="clearContext">Clear Context</v-btn>
              <v-spacer />
              <v-btn variant="text" @click="actAsMenu = false">Close</v-btn>
            </v-card-actions>
          </v-card>
        </v-menu>
        <v-chip v-if="auth.email" class="mx-2" color="primary" variant="tonal">
          {{ auth.email }}
        </v-chip>
        <v-btn variant="text" @click="logout">Logout</v-btn>
      </v-app-bar>
      <v-container class="py-6">
        <router-view />
      </v-container>
    </v-main>
  </v-layout>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { api } from '../plugins/axios'
import { useNotifyStore } from '../stores/notify'

interface CompanyOption {
  title: string
  value: string
}

interface CompanySummary {
  userId: string
  email: string
  companyName: string
}

interface ExpertSummary {
  userId: string
  email: string
  fullName: string
  isApproved: boolean
}

interface PagedResult<T> {
  items: T[]
}

const auth = useAuthStore()
const router = useRouter()
const notify = useNotifyStore()

const actAsMenu = ref(false)
const selectedCompanyId = ref<string | null>(auth.actingCompanyUserId)
const selectedExpertId = ref<string | null>(auth.actingExpertUserId)
const companies = ref<CompanySummary[]>([])
const experts = ref<ExpertSummary[]>([])
const loadingCompanies = ref(false)
const loadingExperts = ref(false)

const adminLinks = [
  { title: 'Dashboard', to: '/admin' },
  { title: 'Users & Roles', to: '/admin/users' },
  { title: 'Experts', to: '/admin/experts' },
  { title: 'Skills', to: '/admin/skills' },
  { title: 'Services', to: '/admin/services' },
  { title: 'Company Assets', to: '/admin/assets' },
  { title: 'Contracts', to: '/admin/contracts' },
  { title: 'Tickets', to: '/admin/tickets' },
  { title: 'Reports', to: '/admin/reports' },
]

const companyLinks = [
  { title: 'Dashboard', to: '/company' },
  { title: 'Assets', to: '/company/assets' },
  { title: 'Contracts', to: '/company/contracts' },
  { title: 'Tickets', to: '/company/tickets' },
  { title: 'Satisfaction', to: '/company/satisfaction' },
]

const expertLinks = [
  { title: 'Dashboard', to: '/expert' },
  { title: 'Assets', to: '/expert/assets' },
  { title: 'Tickets', to: '/expert/tickets' },
  { title: 'Time Logs', to: '/expert/timelogs' },
  { title: 'KPI', to: '/expert/kpi' },
]

const companyOptions = computed<CompanyOption[]>(() =>
  companies.value.map((company) => ({
    title: `${company.companyName} (${company.email})`,
    value: company.userId,
  }))
)

const expertOptions = computed<CompanyOption[]>(() =>
  experts.value.map((expert) => ({
    title: `${expert.fullName} (${expert.email})`,
    value: expert.userId,
  }))
)

const actingLabel = computed(() => {
  if (auth.isActingAsCompany && auth.actingCompanyLabel) {
    return `Acting as Company: ${auth.actingCompanyLabel}`
  }
  if (auth.isActingAsExpert && auth.actingExpertLabel) {
    return `Acting as Expert: ${auth.actingExpertLabel}`
  }
  return ''
})

const loadCompanies = async () => {
  loadingCompanies.value = true
  try {
    const response = await api.get<PagedResult<CompanySummary>>('/admin/companies', {
      params: { page: 1, pageSize: 200 },
    })
    companies.value = response.data.items
  } catch (error) {
    notify.notify('Failed to load companies.', 'error')
  } finally {
    loadingCompanies.value = false
  }
}

const loadExperts = async () => {
  loadingExperts.value = true
  try {
    const response = await api.get<PagedResult<ExpertSummary>>('/admin/experts', {
      params: { page: 1, pageSize: 200 },
    })
    experts.value = response.data.items
  } catch (error) {
    notify.notify('Failed to load experts.', 'error')
  } finally {
    loadingExperts.value = false
  }
}

const applyCompanyContext = () => {
  if (!selectedCompanyId.value) {
    notify.notify('Select a company first.', 'warning')
    return
  }
  const option = companyOptions.value.find((x) => x.value === selectedCompanyId.value)
  auth.setActingCompany(selectedCompanyId.value, option?.title ?? selectedCompanyId.value)
  actAsMenu.value = false
  router.push('/company')
}

const applyExpertContext = () => {
  if (!selectedExpertId.value) {
    notify.notify('Select an expert first.', 'warning')
    return
  }
  const option = expertOptions.value.find((x) => x.value === selectedExpertId.value)
  auth.setActingExpert(selectedExpertId.value, option?.title ?? selectedExpertId.value)
  actAsMenu.value = false
  router.push('/expert')
}

const clearContext = () => {
  auth.clearActingContext()
  actAsMenu.value = false
  router.push('/admin')
}

const logout = () => {
  auth.logout()
  router.push('/login')
}

onMounted(() => {
  void loadCompanies()
  void loadExperts()
})
</script>
