<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">Reports</h2>
        <div class="text-body-2 text-medium-emphasis">Track operations, SLA, and performance trends.</div>
      </div>
      <v-btn color="primary" variant="tonal" :loading="loading" @click="refreshActive">Refresh</v-btn>
    </div>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mb-4">
      {{ errorMessage }}
    </v-alert>

    <v-tabs v-model="activeTab" color="primary" class="mb-4">
      <v-tab value="company">Tickets by company</v-tab>
      <v-tab value="sla">SLA breaches</v-tab>
      <v-tab value="experts">Expert performance</v-tab>
      <v-tab value="services">Top services</v-tab>
    </v-tabs>

    <v-window v-model="activeTab">
      <v-window-item value="company">
        <v-card rounded="lg" elevation="2">
          <v-table>
            <thead>
              <tr>
                <th class="text-left">Company</th>
                <th class="text-left">Total</th>
                <th class="text-left">Open</th>
                <th class="text-left">In Progress</th>
                <th class="text-left">Waiting</th>
                <th class="text-left">Resolved</th>
                <th class="text-left">Closed</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!loading && ticketsByCompany.items.length === 0">
                <td colspan="7" class="text-center py-6">No data found.</td>
              </tr>
              <tr v-for="row in ticketsByCompany.items" :key="row.companyProfileId">
                <td>
                  <div class="font-weight-medium">{{ row.companyName }}</div>
                </td>
                <td>{{ row.totalTickets }}</td>
                <td>{{ row.openTickets }}</td>
                <td>{{ row.inProgressTickets }}</td>
                <td>{{ row.waitingTickets }}</td>
                <td>{{ row.resolvedTickets }}</td>
                <td>{{ row.closedTickets }}</td>
              </tr>
            </tbody>
          </v-table>
          <v-divider />
          <v-card-actions class="d-flex justify-space-between align-center">
            <div class="text-body-2 text-medium-emphasis">Total: {{ ticketsByCompany.totalCount }}</div>
            <v-pagination
              v-model="ticketsByCompany.page"
              :length="ticketsByCompanyTotalPages"
              @update:model-value="loadTicketsByCompany"
            />
          </v-card-actions>
        </v-card>
      </v-window-item>

      <v-window-item value="sla">
        <v-card rounded="lg" elevation="2">
          <v-table>
            <thead>
              <tr>
                <th class="text-left">Ticket</th>
                <th class="text-left">Company</th>
                <th class="text-left">Service</th>
                <th class="text-left">Breach type</th>
                <th class="text-left">Breached at (UTC)</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!loading && slaBreaches.items.length === 0">
                <td colspan="5" class="text-center py-6">No data found.</td>
              </tr>
              <tr v-for="row in slaBreaches.items" :key="row.ticketId">
                <td>{{ row.ticketTitle }}</td>
                <td>{{ row.companyName }}</td>
                <td>{{ row.serviceName || '-' }}</td>
                <td>{{ row.breachType }}</td>
                <td>{{ formatUtc(row.breachedAtUtc) }}</td>
              </tr>
            </tbody>
          </v-table>
          <v-divider />
          <v-card-actions class="d-flex justify-space-between align-center">
            <div class="text-body-2 text-medium-emphasis">Total: {{ slaBreaches.totalCount }}</div>
            <v-pagination
              v-model="slaBreaches.page"
              :length="slaBreachesTotalPages"
              @update:model-value="loadSlaBreaches"
            />
          </v-card-actions>
        </v-card>
      </v-window-item>

      <v-window-item value="experts">
        <v-card rounded="lg" elevation="2">
          <v-table>
            <thead>
              <tr>
                <th class="text-left">Expert</th>
                <th class="text-left">Assigned</th>
                <th class="text-left">Resolved</th>
                <th class="text-left">Avg response (min)</th>
                <th class="text-left">Avg resolution (min)</th>
                <th class="text-left">SLA breaches</th>
                <th class="text-left">Avg rating</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!loading && expertPerformance.items.length === 0">
                <td colspan="7" class="text-center py-6">No data found.</td>
              </tr>
              <tr v-for="row in expertPerformance.items" :key="row.expertProfileId">
                <td>{{ row.expertName }}</td>
                <td>{{ row.assignedTickets }}</td>
                <td>{{ row.resolvedTickets }}</td>
                <td>{{ formatNumber(row.averageFirstResponseMinutes) }}</td>
                <td>{{ formatNumber(row.averageResolutionMinutes) }}</td>
                <td>{{ row.firstResponseBreaches + row.resolutionBreaches }}</td>
                <td>{{ formatNumber(row.averageRating) }}</td>
              </tr>
            </tbody>
          </v-table>
          <v-divider />
          <v-card-actions class="d-flex justify-space-between align-center">
            <div class="text-body-2 text-medium-emphasis">Total: {{ expertPerformance.totalCount }}</div>
            <v-pagination
              v-model="expertPerformance.page"
              :length="expertPerformanceTotalPages"
              @update:model-value="loadExpertPerformance"
            />
          </v-card-actions>
        </v-card>
      </v-window-item>

      <v-window-item value="services">
        <v-card rounded="lg" elevation="2">
          <v-table>
            <thead>
              <tr>
                <th class="text-left">Service</th>
                <th class="text-left">Tickets</th>
                <th class="text-left">Avg resolution (min)</th>
                <th class="text-left">SLA breaches</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!loading && topServices.items.length === 0">
                <td colspan="4" class="text-center py-6">No data found.</td>
              </tr>
              <tr v-for="row in topServices.items" :key="row.serviceId">
                <td>{{ row.serviceName }}</td>
                <td>{{ row.ticketCount }}</td>
                <td>{{ formatNumber(row.averageResolutionMinutes) }}</td>
                <td>{{ row.breachCount }}</td>
              </tr>
            </tbody>
          </v-table>
          <v-divider />
          <v-card-actions class="d-flex justify-space-between align-center">
            <div class="text-body-2 text-medium-emphasis">Total: {{ topServices.totalCount }}</div>
            <v-pagination
              v-model="topServices.page"
              :length="topServicesTotalPages"
              @update:model-value="loadTopServices"
            />
          </v-card-actions>
        </v-card>
      </v-window-item>
    </v-window>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { api } from '../../plugins/axios'
import { useNotifyStore } from '../../stores/notify'

interface PagedResult<T> {
  page: number
  pageSize: number
  totalCount: number
  items: T[]
}

interface TicketsByCompanyRow {
  companyProfileId: string
  companyName: string
  totalTickets: number
  openTickets: number
  inProgressTickets: number
  waitingTickets: number
  resolvedTickets: number
  closedTickets: number
}

interface SlaBreachRow {
  ticketId: string
  ticketTitle: string
  companyName: string
  serviceName?: string | null
  breachType: string
  breachedAtUtc?: string | null
}

interface ExpertPerformanceRow {
  expertProfileId: string
  expertName: string
  assignedTickets: number
  resolvedTickets: number
  averageFirstResponseMinutes?: number | null
  averageResolutionMinutes?: number | null
  firstResponseBreaches: number
  resolutionBreaches: number
  averageRating?: number | null
}

interface TopServiceRow {
  serviceId: string
  serviceName: string
  ticketCount: number
  averageResolutionMinutes?: number | null
  breachCount: number
}

const notify = useNotifyStore()
const activeTab = ref<'company' | 'sla' | 'experts' | 'services'>('company')
const loading = ref(false)
const errorMessage = ref('')

const ticketsByCompany = ref<PagedResult<TicketsByCompanyRow>>({
  page: 1,
  pageSize: 10,
  totalCount: 0,
  items: [],
})

const slaBreaches = ref<PagedResult<SlaBreachRow>>({
  page: 1,
  pageSize: 10,
  totalCount: 0,
  items: [],
})

const expertPerformance = ref<PagedResult<ExpertPerformanceRow>>({
  page: 1,
  pageSize: 10,
  totalCount: 0,
  items: [],
})

const topServices = ref<PagedResult<TopServiceRow>>({
  page: 1,
  pageSize: 10,
  totalCount: 0,
  items: [],
})

const ticketsByCompanyLoaded = ref(false)
const slaBreachesLoaded = ref(false)
const expertPerformanceLoaded = ref(false)
const topServicesLoaded = ref(false)

const formatUtc = (value?: string | null) => {
  if (!value) return '-'
  return new Date(value).toISOString().replace('T', ' ').replace('Z', ' UTC')
}

const formatNumber = (value?: number | null) => {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return '-'
  }
  return value.toFixed(1)
}

const totalPages = (result: PagedResult<unknown>) =>
  Math.max(1, Math.ceil(result.totalCount / result.pageSize))

const loadTicketsByCompany = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const response = await api.get<PagedResult<TicketsByCompanyRow>>('/admin/reports/tickets-by-company', {
      params: { page: ticketsByCompany.value.page, pageSize: ticketsByCompany.value.pageSize },
    })
    ticketsByCompany.value = response.data
    ticketsByCompanyLoaded.value = true
  } catch (error) {
    errorMessage.value = 'Failed to load tickets by company.'
    notify.notify('Failed to load tickets by company.', 'error')
  } finally {
    loading.value = false
  }
}

const loadSlaBreaches = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const response = await api.get<PagedResult<SlaBreachRow>>('/admin/reports/sla-breaches', {
      params: { page: slaBreaches.value.page, pageSize: slaBreaches.value.pageSize },
    })
    slaBreaches.value = response.data
    slaBreachesLoaded.value = true
  } catch (error) {
    errorMessage.value = 'Failed to load SLA breaches.'
    notify.notify('Failed to load SLA breaches.', 'error')
  } finally {
    loading.value = false
  }
}

const loadExpertPerformance = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const response = await api.get<PagedResult<ExpertPerformanceRow>>('/admin/reports/expert-performance', {
      params: { page: expertPerformance.value.page, pageSize: expertPerformance.value.pageSize },
    })
    expertPerformance.value = response.data
    expertPerformanceLoaded.value = true
  } catch (error) {
    errorMessage.value = 'Failed to load expert performance.'
    notify.notify('Failed to load expert performance.', 'error')
  } finally {
    loading.value = false
  }
}

const loadTopServices = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const response = await api.get<PagedResult<TopServiceRow>>('/admin/reports/top-services', {
      params: { page: topServices.value.page, pageSize: topServices.value.pageSize },
    })
    topServices.value = response.data
    topServicesLoaded.value = true
  } catch (error) {
    errorMessage.value = 'Failed to load top services.'
    notify.notify('Failed to load top services.', 'error')
  } finally {
    loading.value = false
  }
}

const ensureActiveTabLoaded = async () => {
  if (activeTab.value === 'company' && !ticketsByCompanyLoaded.value) {
    await loadTicketsByCompany()
  } else if (activeTab.value === 'sla' && !slaBreachesLoaded.value) {
    await loadSlaBreaches()
  } else if (activeTab.value === 'experts' && !expertPerformanceLoaded.value) {
    await loadExpertPerformance()
  } else if (activeTab.value === 'services' && !topServicesLoaded.value) {
    await loadTopServices()
  }
}

const refreshActive = async () => {
  if (activeTab.value === 'company') {
    await loadTicketsByCompany()
  } else if (activeTab.value === 'sla') {
    await loadSlaBreaches()
  } else if (activeTab.value === 'experts') {
    await loadExpertPerformance()
  } else if (activeTab.value === 'services') {
    await loadTopServices()
  }
}

const ticketsByCompanyTotalPages = computed(() => totalPages(ticketsByCompany.value))
const slaBreachesTotalPages = computed(() => totalPages(slaBreaches.value))
const expertPerformanceTotalPages = computed(() => totalPages(expertPerformance.value))
const topServicesTotalPages = computed(() => totalPages(topServices.value))

onMounted(() => {
  void ensureActiveTabLoaded()
})

watch(activeTab, () => {
  void ensureActiveTabLoaded()
})
</script>
