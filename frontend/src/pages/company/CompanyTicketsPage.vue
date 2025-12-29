<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">Tickets</h2>
        <div class="text-body-2 text-medium-emphasis">Create and track support tickets.</div>
      </div>
      <v-btn color="primary" @click="openCreate">New Ticket</v-btn>
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
            <th class="text-left">Service</th>
            <th class="text-left">Asset</th>
            <th class="text-left">Status</th>
            <th class="text-left">Assigned</th>
            <th class="text-left">Created (UTC)</th>
            <th class="text-left">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!loading && tickets.length === 0">
            <td colspan="7" class="text-center py-6">No tickets found.</td>
          </tr>
          <tr v-for="ticket in tickets" :key="ticket.id">
            <td>{{ ticket.title }}</td>
            <td>{{ ticket.serviceName || '-' }}</td>
            <td>{{ ticket.assetName || '-' }}</td>
            <td>
              <v-chip :color="statusColor(ticket.status)" size="small" variant="tonal">
                {{ statusLabel(ticket.status) }}
              </v-chip>
            </td>
            <td>{{ ticket.assignedExpertName || 'Unassigned' }}</td>
            <td>{{ formatUtc(ticket.createdAtUtc) }}</td>
            <td>
              <v-btn size="small" variant="text" @click="openDetails(ticket)">View</v-btn>
              <v-btn
                v-if="ticket.status === 4"
                size="small"
                variant="text"
                color="primary"
                :loading="closingId === ticket.id"
                @click="closeTicket(ticket)"
              >
                Close
              </v-btn>
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

    <v-dialog v-model="createOpen" max-width="720">
      <v-card>
        <v-card-title>Create Ticket</v-card-title>
        <v-card-text>
          <v-form @submit.prevent="submitTicket">
            <v-text-field v-model="title" label="Title" :error-messages="titleError" required />
            <v-textarea v-model="description" label="Description" :error-messages="descriptionError" rows="4" required />
            <v-row dense>
              <v-col cols="12" md="6">
                <v-select
                  v-model="serviceId"
                  :items="serviceOptions"
                  item-title="title"
                  item-value="value"
                  label="Service (optional)"
                  clearable
                  :error-messages="serviceIdError"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-select
                  v-model="assetId"
                  :items="assetOptions"
                  item-title="title"
                  item-value="value"
                  label="Asset (optional)"
                  clearable
                  :error-messages="assetIdError"
                />
              </v-col>
            </v-row>
            <div class="text-body-2 text-medium-emphasis">
              At least one of Service or Asset is required.
            </div>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="createOpen = false">Cancel</v-btn>
          <v-btn color="primary" :loading="isSubmitting" @click="submitTicket">Create</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="detailsOpen" max-width="900">
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
              <div class="text-body-2 text-medium-emphasis">Service</div>
              <div class="text-subtitle-2">{{ selectedTicket.serviceName || '-' }}</div>
            </v-col>
            <v-col cols="12" md="6">
              <div class="text-body-2 text-medium-emphasis">Asset</div>
              <div class="text-subtitle-2">{{ selectedTicket.assetName || '-' }}</div>
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

          <div class="text-subtitle-1 font-weight-medium mb-2">Messages</div>
          <v-list density="compact" class="mb-3">
            <v-list-item v-for="message in messages" :key="message.id">
              <v-list-item-title>
                {{ messageLabel(message.authorRole) }}
                <span class="text-body-2 text-medium-emphasis ml-2">{{ formatUtc(message.createdAtUtc) }}</span>
              </v-list-item-title>
              <v-list-item-subtitle>{{ message.body }}</v-list-item-subtitle>
            </v-list-item>
            <v-list-item v-if="messages.length === 0">
              <v-list-item-title>No messages yet.</v-list-item-title>
            </v-list-item>
          </v-list>

          <v-form @submit.prevent="submitMessage">
            <v-textarea v-model="messageBody" label="Reply" rows="3" />
            <v-btn color="primary" :loading="sendingMessage" @click="submitMessage">Send</v-btn>
          </v-form>

          <v-divider class="my-4" />

          <v-card variant="outlined" rounded="lg">
            <v-card-text>
              <div class="text-subtitle-1 font-weight-medium mb-2">Satisfaction</div>
              <div v-if="selectedTicket.status !== 5" class="text-body-2 text-medium-emphasis">
                Available after the ticket is closed.
              </div>
              <div v-else-if="isSatisfactionSubmitted(selectedTicket.id)" class="text-body-2 text-medium-emphasis">
                Satisfaction already submitted. Thank you!
              </div>
              <v-form v-else @submit.prevent="submitSatisfaction">
                <v-row dense>
                  <v-col cols="12" md="6">
                    <v-select
                      v-model="satisfactionRating"
                      :items="ratingOptions"
                      label="Overall rating (1-5)"
                      :error-messages="satisfactionRatingError"
                      required
                    />
                  </v-col>
                  <v-col cols="12" md="6">
                    <v-select
                      v-model="responseTimeRating"
                      :items="ratingOptions"
                      label="Response time (optional)"
                      clearable
                      :error-messages="responseTimeRatingError"
                    />
                  </v-col>
                  <v-col cols="12" md="6">
                    <v-select
                      v-model="resolutionQualityRating"
                      :items="ratingOptions"
                      label="Resolution quality (optional)"
                      clearable
                      :error-messages="resolutionQualityRatingError"
                    />
                  </v-col>
                  <v-col cols="12" md="6">
                    <v-select
                      v-model="communicationRating"
                      :items="ratingOptions"
                      label="Communication (optional)"
                      clearable
                      :error-messages="communicationRatingError"
                    />
                  </v-col>
                  <v-col cols="12">
                    <v-textarea
                      v-model="satisfactionComment"
                      label="Comment (optional)"
                      rows="3"
                      :error-messages="satisfactionCommentError"
                    />
                  </v-col>
                </v-row>
                <v-btn color="primary" :loading="satisfactionSubmitting" @click="submitSatisfaction">
                  Submit satisfaction
                </v-btn>
              </v-form>
            </v-card-text>
          </v-card>
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
import { computed, onMounted, ref } from 'vue'
import { useForm, useField } from 'vee-validate'
import * as yup from 'yup'
import { api } from '../../plugins/axios'
import { useNotifyStore } from '../../stores/notify'

interface TicketDto {
  id: string
  title: string
  description: string
  status: number
  companyProfileId: string
  companyName: string
  serviceId?: string | null
  serviceName?: string | null
  assetId?: string | null
  assetName?: string | null
  assignedExpertProfileId?: string | null
  assignedExpertName?: string | null
  slaFirstResponseMinutes: number
  slaResolutionMinutes: number
  firstResponseDueAtUtc: string
  resolutionDueAtUtc: string
  firstResponseAtUtc?: string | null
  resolvedAtUtc?: string | null
  closedAtUtc?: string | null
  firstResponseBreached: boolean
  resolutionBreached: boolean
  createdAtUtc: string
}

interface TicketMessageDto {
  id: string
  authorRole: number
  body: string
  createdAtUtc: string
}

interface ServiceDto {
  id: string
  name: string
  description?: string | null
}

interface AssetDto {
  id: string
  name: string
  assetTag?: string | null
  serialNumber?: string | null
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

const createOpen = ref(false)
const services = ref<ServiceDto[]>([])
const assets = ref<AssetDto[]>([])

const detailsOpen = ref(false)
const selectedTicket = ref<TicketDto | null>(null)
const messages = ref<TicketMessageDto[]>([])
const messageBody = ref('')
const sendingMessage = ref(false)
const closingId = ref<string | null>(null)
const satisfactionSubmittedIds = ref(new Set<string>())

const schema = yup
  .object({
    title: yup.string().required().max(200),
    description: yup.string().required().max(4000),
    serviceId: yup.string().nullable(),
    assetId: yup.string().nullable(),
  })
  .test('service-asset', 'Service or Asset is required.', (value) => {
    return !!value?.serviceId || !!value?.assetId
  })

const { handleSubmit, resetForm, isSubmitting } = useForm({
  validationSchema: schema,
  initialValues: {
    title: '',
    description: '',
    serviceId: null,
    assetId: null,
  },
})

const { value: title, errorMessage: titleError } = useField<string>('title')
const { value: description, errorMessage: descriptionError } = useField<string>('description')
const { value: serviceId, errorMessage: serviceIdError } = useField<string | null>('serviceId')
const { value: assetId, errorMessage: assetIdError } = useField<string | null>('assetId')

const satisfactionSchema = yup.object({
  rating: yup.number().required().min(1).max(5),
  responseTimeRating: yup.number().nullable().min(1).max(5),
  resolutionQualityRating: yup.number().nullable().min(1).max(5),
  communicationRating: yup.number().nullable().min(1).max(5),
  comment: yup.string().nullable().max(1000),
})

const satisfactionForm = useForm({
  validationSchema: satisfactionSchema,
  initialValues: {
    rating: 5,
    responseTimeRating: null,
    resolutionQualityRating: null,
    communicationRating: null,
    comment: '',
  },
})

const {
  handleSubmit: handleSatisfactionSubmit,
  resetForm: resetSatisfactionForm,
  isSubmitting: satisfactionSubmitting,
} = satisfactionForm

const { value: satisfactionRating, errorMessage: satisfactionRatingError } = useField<number>(
  'rating',
  undefined,
  { form: satisfactionForm }
)
const { value: responseTimeRating, errorMessage: responseTimeRatingError } = useField<number | null>(
  'responseTimeRating',
  undefined,
  { form: satisfactionForm }
)
const { value: resolutionQualityRating, errorMessage: resolutionQualityRatingError } = useField<number | null>(
  'resolutionQualityRating',
  undefined,
  { form: satisfactionForm }
)
const { value: communicationRating, errorMessage: communicationRatingError } = useField<number | null>(
  'communicationRating',
  undefined,
  { form: satisfactionForm }
)
const { value: satisfactionComment, errorMessage: satisfactionCommentError } = useField<string | null>(
  'comment',
  undefined,
  { form: satisfactionForm }
)

const serviceOptions = computed(() =>
  services.value.map((service) => ({
    title: service.name,
    value: service.id,
  }))
)

const assetOptions = computed(() =>
  assets.value.map((asset) => ({
    title: `${asset.name} (${asset.assetTag || '-'}, ${asset.serialNumber || '-'})`,
    value: asset.id,
  }))
)

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

const ratingOptions = [
  { title: '1', value: 1 },
  { title: '2', value: 2 },
  { title: '3', value: 3 },
  { title: '4', value: 4 },
  { title: '5', value: 5 },
]

const messageLabel = (role: number) => {
  if (role === 1) return 'Company'
  if (role === 2) return 'Expert'
  if (role === 3) return 'Admin'
  return 'Unknown'
}

const formatUtc = (value?: string | null) => {
  if (!value) return '-'
  return new Date(value).toISOString().replace('T', ' ').replace('Z', ' UTC')
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
    const response = await api.get<PagedResult<TicketDto>>('/company/tickets', { params })
    tickets.value = response.data.items
    totalCount.value = response.data.totalCount
  } catch (error) {
    notify.notify('Failed to load tickets.', 'error')
  } finally {
    loading.value = false
  }
}

const loadServices = async () => {
  try {
    const response = await api.get<PagedResult<ServiceDto>>('/services', {
      params: { page: 1, pageSize: 200 },
    })
    services.value = response.data.items
  } catch (error) {
    notify.notify('Failed to load services.', 'error')
  }
}

const loadAssets = async () => {
  try {
    const response = await api.get<PagedResult<AssetDto>>('/company/assets', {
      params: { page: 1, pageSize: 200 },
    })
    assets.value = response.data.items
  } catch (error) {
    notify.notify('Failed to load assets.', 'error')
  }
}

const applyFilters = () => {
  page.value = 1
  void loadTickets()
}

const openCreate = async () => {
  resetForm()
  await Promise.all([loadServices(), loadAssets()])
  createOpen.value = true
}

const submitTicket = handleSubmit(async (values) => {
  const payload = {
    title: values.title,
    description: values.description,
    serviceId: values.serviceId || null,
    assetId: values.assetId || null,
  }
  try {
    await api.post('/company/tickets', payload)
    notify.notify('Ticket created.', 'success')
    createOpen.value = false
    await loadTickets()
  } catch (error) {
    notify.notify('Failed to create ticket.', 'error')
  }
})

const openDetails = async (ticket: TicketDto) => {
  selectedTicket.value = ticket
  detailsOpen.value = true
  resetSatisfactionForm()
  await loadMessages(ticket.id)
}

const loadMessages = async (ticketId: string) => {
  try {
    const response = await api.get<TicketMessageDto[]>(`/company/tickets/${ticketId}/messages`)
    messages.value = response.data
  } catch (error) {
    notify.notify('Failed to load messages.', 'error')
  }
}

const submitMessage = async () => {
  if (!selectedTicket.value || !messageBody.value.trim()) {
    return
  }
  sendingMessage.value = true
  try {
    await api.post(`/company/tickets/${selectedTicket.value.id}/messages`, {
      body: messageBody.value.trim(),
      isInternal: false,
    })
    messageBody.value = ''
    await loadMessages(selectedTicket.value.id)
  } catch (error) {
    notify.notify('Failed to send message.', 'error')
  } finally {
    sendingMessage.value = false
  }
}

const isSatisfactionSubmitted = (ticketId: string) => satisfactionSubmittedIds.value.has(ticketId)

const markSatisfactionSubmitted = (ticketId: string) => {
  const next = new Set(satisfactionSubmittedIds.value)
  next.add(ticketId)
  satisfactionSubmittedIds.value = next
}

const submitSatisfaction = handleSatisfactionSubmit(async (values) => {
  if (!selectedTicket.value) return
  try {
    await api.post(`/company/tickets/${selectedTicket.value.id}/satisfaction`, {
      rating: values.rating,
      responseTimeRating: values.responseTimeRating,
      resolutionQualityRating: values.resolutionQualityRating,
      communicationRating: values.communicationRating,
      comment: values.comment?.trim() || null,
    })
    markSatisfactionSubmitted(selectedTicket.value.id)
    notify.notify('Satisfaction submitted. Thank you!', 'success')
  } catch (error: any) {
    if (error?.response?.status === 400) {
      markSatisfactionSubmitted(selectedTicket.value.id)
    }
    notify.notify('Failed to submit satisfaction.', 'error')
  }
})

const closeTicket = async (ticket: TicketDto) => {
  closingId.value = ticket.id
  try {
    await api.patch(`/company/tickets/${ticket.id}/status`, { status: 5 })
    notify.notify('Ticket closed.', 'success')
    await loadTickets()
  } catch (error) {
    notify.notify('Failed to close ticket.', 'error')
  } finally {
    closingId.value = null
  }
}

onMounted(() => {
  void loadTickets()
})
</script>
