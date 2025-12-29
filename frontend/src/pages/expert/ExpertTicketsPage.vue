<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">My Tickets</h2>
        <div class="text-body-2 text-medium-emphasis">Work on assigned tickets and update status.</div>
      </div>
      <v-btn variant="tonal" @click="loadTickets" :loading="loading">Refresh</v-btn>
    </div>

    <v-card class="mb-4" rounded="lg" elevation="2">
      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="6">
            <v-text-field
              v-model="search"
              label="Search tickets"
              clearable
              @keyup.enter="applyFilters"
            />
          </v-col>
          <v-col cols="12" md="4">
            <v-select v-model="statusFilter" :items="statusOptions" label="Status" />
          </v-col>
          <v-col cols="12" md="2" class="d-flex align-end">
            <v-btn block color="primary" variant="tonal" @click="applyFilters">Apply</v-btn>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-card rounded="lg" elevation="2">
      <v-table>
        <thead>
          <tr>
            <th class="text-left">Title</th>
            <th class="text-left">Company</th>
            <th class="text-left">Status</th>
            <th class="text-left">SLA</th>
            <th class="text-left">Created (UTC)</th>
            <th class="text-left">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!loading && tickets.length === 0">
            <td colspan="6" class="text-center py-6">No tickets found.</td>
          </tr>
          <tr v-for="ticket in tickets" :key="ticket.id">
            <td>{{ ticket.title }}</td>
            <td>{{ ticket.companyName }}</td>
            <td>
              <v-chip :color="statusColor(ticket.status)" size="small" variant="tonal">
                {{ statusLabel(ticket.status) }}
              </v-chip>
            </td>
            <td>
              <v-chip :color="ticket.firstResponseBreached ? 'error' : 'success'" size="x-small" class="mr-1">
                FR
              </v-chip>
              <v-chip :color="ticket.resolutionBreached ? 'error' : 'success'" size="x-small">
                RES
              </v-chip>
            </td>
            <td>{{ formatUtc(ticket.createdAtUtc) }}</td>
            <td>
              <v-btn size="small" variant="text" @click="openDetails(ticket)">View</v-btn>
            </td>
          </tr>
        </tbody>
      </v-table>
      <v-divider />
      <v-card-actions class="d-flex justify-space-between align-center">
        <div class="text-body-2 text-medium-emphasis">
          Total: {{ totalCount }}
        </div>
        <v-pagination v-model="page" :length="totalPages" @update:model-value="loadTickets" />
      </v-card-actions>
    </v-card>

    <v-dialog v-model="detailsOpen" max-width="980">
      <v-card>
        <v-card-title>Ticket Details</v-card-title>
        <v-card-text v-if="selectedTicket">
          <v-row dense class="mb-3">
            <v-col cols="12" md="6">
              <div class="text-body-2 text-medium-emphasis">Title</div>
              <div class="text-subtitle-1 font-weight-medium">{{ selectedTicket.title }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-body-2 text-medium-emphasis">Status</div>
              <v-chip :color="statusColor(selectedTicket.status)" size="small" variant="tonal">
                {{ statusLabel(selectedTicket.status) }}
              </v-chip>
            </v-col>
          </v-row>

          <v-row dense class="mb-3">
            <v-col cols="12" md="6">
              <div class="text-body-2 text-medium-emphasis">Company</div>
              <div class="text-subtitle-2">{{ selectedTicket.companyName }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-body-2 text-medium-emphasis">Service / Asset</div>
              <div class="text-subtitle-2">
                {{ selectedTicket.serviceName || '-' }} / {{ selectedTicket.assetName || '-' }}
              </div>
            </v-col>
          </v-row>

          <v-card variant="outlined" rounded="lg" class="mb-4">
            <v-card-text>
              <div class="text-subtitle-2 font-weight-medium mb-2">SLA</div>
              <v-row dense>
                <v-col cols="12" md="6">
                  <div class="text-body-2 text-medium-emphasis">First response due</div>
                  <div>{{ formatUtc(selectedTicket.firstResponseDueAtUtc) }}</div>
                  <div class="text-body-2" :class="selectedTicket.firstResponseBreached ? 'text-error' : 'text-success'">
                    {{ selectedTicket.firstResponseBreached ? 'Breached' : 'On track' }}
                  </div>
                </v-col>
                <v-col cols="12" md="6">
                  <div class="text-body-2 text-medium-emphasis">Resolution due</div>
                  <div>{{ formatUtc(selectedTicket.resolutionDueAtUtc) }}</div>
                  <div class="text-body-2" :class="selectedTicket.resolutionBreached ? 'text-error' : 'text-success'">
                    {{ selectedTicket.resolutionBreached ? 'Breached' : 'On track' }}
                  </div>
                </v-col>
              </v-row>
            </v-card-text>
          </v-card>

          <div class="text-subtitle-1 font-weight-medium mb-2">Status Update</div>
          <v-row dense class="mb-4">
            <v-col cols="12" md="6">
              <v-select
                v-model="nextStatus"
                :items="nextStatusOptions"
                item-title="title"
                item-value="value"
                label="Next status"
                :disabled="nextStatusOptions.length === 0"
              />
            </v-col>
            <v-col cols="12" md="6" class="d-flex align-end">
              <v-btn
                color="primary"
                :disabled="!nextStatus"
                :loading="updatingStatus"
                @click="updateStatus"
              >
                Update Status
              </v-btn>
            </v-col>
          </v-row>

          <div class="text-subtitle-1 font-weight-medium mb-2">Messages</div>
          <v-list density="compact" class="mb-3">
            <v-list-item v-for="message in messages" :key="message.id">
              <v-list-item-title>
                {{ messageLabel(message.authorRole) }}
                <v-chip v-if="message.isInternal" size="x-small" class="ml-2">Internal</v-chip>
                <span class="text-body-2 text-medium-emphasis ml-2">{{ formatUtc(message.createdAtUtc) }}</span>
              </v-list-item-title>
              <v-list-item-subtitle>{{ message.body }}</v-list-item-subtitle>
            </v-list-item>
            <v-list-item v-if="messages.length === 0">
              <v-list-item-title>No messages yet.</v-list-item-title>
            </v-list-item>
          </v-list>

          <v-form @submit.prevent="submitMessage" class="mb-6">
            <v-textarea v-model="messageBody" label="Reply" rows="3" />
            <v-checkbox v-model="messageInternal" label="Internal note" hide-details />
            <v-btn color="primary" :loading="sendingMessage" @click="submitMessage">Send</v-btn>
          </v-form>

          <div class="text-subtitle-1 font-weight-medium mb-2">Time Logs</div>
          <v-table class="mb-3">
            <thead>
              <tr>
                <th class="text-left">Minutes</th>
                <th class="text-left">Work Type</th>
                <th class="text-left">Logged At (UTC)</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="timeLogs.length === 0">
                <td colspan="3" class="text-center py-4">No time logs.</td>
              </tr>
              <tr v-for="log in timeLogs" :key="log.id">
                <td>{{ log.minutes }}</td>
                <td>{{ workTypeLabel(log.workType) }}</td>
                <td>{{ formatUtc(log.loggedAtUtc) }}</td>
              </tr>
            </tbody>
          </v-table>

          <v-form @submit.prevent="submitTimeLog">
            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field v-model.number="timeLogMinutes" label="Minutes" type="number" />
              </v-col>
              <v-col cols="12" md="4">
                <v-select
                  v-model="timeLogWorkType"
                  :items="workTypeOptions"
                  item-title="title"
                  item-value="value"
                  label="Work type"
                />
              </v-col>
              <v-col cols="12" md="4" class="d-flex align-end">
                <v-btn color="primary" :loading="addingTimeLog" @click="submitTimeLog">Add Log</v-btn>
              </v-col>
            </v-row>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="detailsOpen = false">Close</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { api } from '../../plugins/axios'
import { useNotifyStore } from '../../stores/notify'

interface TicketDto {
  id: string
  title: string
  status: number
  companyName: string
  serviceName?: string | null
  assetName?: string | null
  firstResponseDueAtUtc: string
  resolutionDueAtUtc: string
  firstResponseBreached: boolean
  resolutionBreached: boolean
  createdAtUtc: string
}

interface TicketMessageDto {
  id: string
  authorRole: number
  body: string
  isInternal: boolean
  createdAtUtc: string
}

interface TicketTimeLogDto {
  id: string
  minutes: number
  workType: number
  loggedAtUtc: string
}

interface PagedResult<T> {
  page: number
  pageSize: number
  totalCount: number
  items: T[]
}

const notify = useNotifyStore()

const tickets = ref<TicketDto[]>([])
const loading = ref(false)
const search = ref('')
const statusFilter = ref<'all' | number>('all')
const statusOptions = [
  { title: 'All', value: 'all' },
  { title: 'Open', value: 1 },
  { title: 'In Progress', value: 2 },
  { title: 'Waiting For Customer', value: 3 },
  { title: 'Resolved', value: 4 },
  { title: 'Closed', value: 5 },
]

const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

const detailsOpen = ref(false)
const selectedTicket = ref<TicketDto | null>(null)
const messages = ref<TicketMessageDto[]>([])
const messageBody = ref('')
const messageInternal = ref(false)
const sendingMessage = ref(false)

const nextStatus = ref<number | null>(null)
const updatingStatus = ref(false)
const nextStatusOptions = ref<{ title: string; value: number }[]>([])

const timeLogs = ref<TicketTimeLogDto[]>([])
const timeLogMinutes = ref<number | null>(null)
const timeLogWorkType = ref<number>(1)
const addingTimeLog = ref(false)
const workTypeOptions = [
  { title: 'Remote', value: 1 },
  { title: 'Onsite', value: 2 },
  { title: 'On Call', value: 3 },
]

const statusLabel = (status: number) => {
  const match = statusOptions.find((option) => option.value === status)
  return match?.title ?? 'Unknown'
}

const statusColor = (status: number) => {
  if (status === 4) return 'success'
  if (status === 5) return 'default'
  if (status === 3) return 'warning'
  return 'primary'
}

const messageLabel = (role: number) => {
  if (role === 1) return 'Company'
  if (role === 2) return 'Expert'
  if (role === 3) return 'Admin'
  return 'Unknown'
}

const workTypeLabel = (value: number) => {
  const match = workTypeOptions.find((option) => option.value === value)
  return match?.title ?? 'Unknown'
}

const formatUtc = (value?: string | null) => {
  if (!value) return '-'
  return new Date(value).toISOString().replace('T', ' ').replace('Z', ' UTC')
}

const getNextStatuses = (status: number) => {
  if (status === 1) return [{ title: 'In Progress', value: 2 }]
  if (status === 2)
    return [
      { title: 'Waiting For Customer', value: 3 },
      { title: 'Resolved', value: 4 },
    ]
  if (status === 3) return [{ title: 'In Progress', value: 2 }]
  if (status === 4) return [{ title: 'Closed', value: 5 }]
  return []
}

const loadTickets = async () => {
  loading.value = true
  try {
    const params: Record<string, string | number> = {
      page: page.value,
      pageSize: pageSize.value,
    }
    if (search.value.trim()) {
      params.search = search.value.trim()
    }
    if (statusFilter.value !== 'all') {
      params.status = Number(statusFilter.value)
    }
    const response = await api.get<PagedResult<TicketDto>>('/expert/tickets', { params })
    tickets.value = response.data.items
    totalCount.value = response.data.totalCount
  } catch (error) {
    notify.notify('Failed to load tickets.', 'error')
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  page.value = 1
  void loadTickets()
}

const openDetails = async (ticket: TicketDto) => {
  selectedTicket.value = ticket
  detailsOpen.value = true
  nextStatusOptions.value = getNextStatuses(ticket.status)
  nextStatus.value = nextStatusOptions.value[0]?.value ?? null
  await Promise.all([loadMessages(ticket.id), loadTimeLogs(ticket.id)])
}

const loadMessages = async (ticketId: string) => {
  try {
    const response = await api.get<TicketMessageDto[]>(`/expert/tickets/${ticketId}/messages`)
    messages.value = response.data
  } catch (error) {
    notify.notify('Failed to load messages.', 'error')
  }
}

const loadTimeLogs = async (ticketId: string) => {
  try {
    const response = await api.get<TicketTimeLogDto[]>(`/expert/tickets/${ticketId}/timelogs`)
    timeLogs.value = response.data
  } catch (error) {
    notify.notify('Failed to load time logs.', 'error')
  }
}

const submitMessage = async () => {
  if (!selectedTicket.value || !messageBody.value.trim()) {
    return
  }
  sendingMessage.value = true
  try {
    await api.post(`/expert/tickets/${selectedTicket.value.id}/messages`, {
      body: messageBody.value.trim(),
      isInternal: messageInternal.value,
    })
    messageBody.value = ''
    messageInternal.value = false
    await loadMessages(selectedTicket.value.id)
  } catch (error) {
    notify.notify('Failed to send message.', 'error')
  } finally {
    sendingMessage.value = false
  }
}

const submitTimeLog = async () => {
  if (!selectedTicket.value || !timeLogMinutes.value || timeLogMinutes.value <= 0) {
    notify.notify('Enter valid minutes.', 'warning')
    return
  }
  addingTimeLog.value = true
  try {
    await api.post(`/expert/tickets/${selectedTicket.value.id}/timelogs`, {
      minutes: timeLogMinutes.value,
      workType: timeLogWorkType.value,
    })
    timeLogMinutes.value = null
    await loadTimeLogs(selectedTicket.value.id)
  } catch (error) {
    notify.notify('Failed to add time log.', 'error')
  } finally {
    addingTimeLog.value = false
  }
}

const updateStatus = async () => {
  if (!selectedTicket.value || !nextStatus.value) {
    return
  }
  updatingStatus.value = true
  try {
    await api.patch(`/expert/tickets/${selectedTicket.value.id}/status`, {
      status: nextStatus.value,
    })
    notify.notify('Status updated.', 'success')
    await loadTickets()
    selectedTicket.value = tickets.value.find((t) => t.id === selectedTicket.value?.id) ?? selectedTicket.value
    if (selectedTicket.value) {
      nextStatusOptions.value = getNextStatuses(selectedTicket.value.status)
      nextStatus.value = nextStatusOptions.value[0]?.value ?? null
    }
  } catch (error) {
    notify.notify('Failed to update status.', 'error')
  } finally {
    updatingStatus.value = false
  }
}

watch(detailsOpen, (open) => {
  if (!open) {
    selectedTicket.value = null
    messages.value = []
    timeLogs.value = []
  }
})

onMounted(() => {
  void loadTickets()
})
</script>
