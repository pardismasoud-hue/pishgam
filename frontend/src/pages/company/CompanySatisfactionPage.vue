<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">Satisfaction</h2>
        <div class="text-body-2 text-medium-emphasis">Submit feedback after tickets are closed.</div>
      </div>
      <v-btn color="primary" variant="tonal" :loading="loading" @click="loadTickets">Refresh</v-btn>
    </div>

    <v-card class="mb-4" rounded="lg" elevation="2">
      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="8">
            <v-text-field
              v-model="search"
              label="Search closed tickets"
              clearable
              @keyup.enter="applyFilters"
            />
          </v-col>
          <v-col cols="12" md="4" class="d-flex align-end">
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
            <th class="text-left">Closed (UTC)</th>
            <th class="text-left">Status</th>
            <th class="text-left">Rating</th>
            <th class="text-left">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!loading && tickets.length === 0">
            <td colspan="7" class="text-center py-6">No closed tickets found.</td>
          </tr>
          <tr v-for="ticket in tickets" :key="ticket.ticketId">
            <td>{{ ticket.title }}</td>
            <td>{{ ticket.serviceName || '-' }}</td>
            <td>{{ ticket.assetName || '-' }}</td>
            <td>{{ formatUtc(ticket.closedAtUtc) }}</td>
            <td>
              <v-chip :color="ticket.satisfactionSubmitted ? 'success' : 'warning'" size="small" variant="tonal">
                {{ ticket.satisfactionSubmitted ? 'Submitted' : 'Pending' }}
              </v-chip>
            </td>
            <td>{{ ticket.rating ?? '-' }}</td>
            <td>
              <v-btn
                v-if="!ticket.satisfactionSubmitted"
                size="small"
                variant="text"
                color="primary"
                @click="openDialog(ticket)"
              >
                Submit
              </v-btn>
            </td>
          </tr>
        </tbody>
      </v-table>
      <v-divider />
      <v-card-actions class="d-flex justify-space-between align-center">
        <div class="text-body-2 text-medium-emphasis">Total: {{ totalCount }}</div>
        <v-pagination v-model="page" :length="totalPages" @update:model-value="loadTickets" />
      </v-card-actions>
    </v-card>

    <v-dialog v-model="dialogOpen" max-width="640">
      <v-card>
        <v-card-title>Submit Satisfaction</v-card-title>
        <v-card-text>
          <div v-if="selectedTicket" class="mb-3">
            <div class="text-body-2 text-medium-emphasis">Ticket</div>
            <div class="text-subtitle-1 font-weight-medium">{{ selectedTicket.title }}</div>
          </div>
          <v-form @submit.prevent="submitSatisfaction">
            <v-select
              v-model="rating"
              :items="ratingOptions"
              label="Overall rating (1-5)"
              :error-messages="ratingError"
              required
            />
            <v-row dense>
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
            </v-row>
            <v-textarea
              v-model="comment"
              label="Comment (optional)"
              rows="3"
              :error-messages="commentError"
            />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="dialogOpen = false">Cancel</v-btn>
          <v-btn color="primary" :loading="submitting" @click="submitSatisfaction">Submit</v-btn>
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

interface ClosedTicketSatisfactionDto {
  ticketId: string
  title: string
  serviceName?: string | null
  assetName?: string | null
  closedAtUtc: string
  satisfactionSubmitted: boolean
  rating?: number | null
  responseTimeRating?: number | null
  resolutionQualityRating?: number | null
  communicationRating?: number | null
  comment?: string | null
}

interface PagedResult<T> {
  page: number
  pageSize: number
  totalCount: number
  items: T[]
}

const notify = useNotifyStore()
const loading = ref(false)
const tickets = ref<ClosedTicketSatisfactionDto[]>([])
const search = ref('')
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

const dialogOpen = ref(false)
const selectedTicket = ref<ClosedTicketSatisfactionDto | null>(null)

const ratingOptions = [
  { title: '1', value: 1 },
  { title: '2', value: 2 },
  { title: '3', value: 3 },
  { title: '4', value: 4 },
  { title: '5', value: 5 },
]

const schema = yup.object({
  rating: yup.number().required().min(1).max(5),
  responseTimeRating: yup.number().nullable().min(1).max(5),
  resolutionQualityRating: yup.number().nullable().min(1).max(5),
  communicationRating: yup.number().nullable().min(1).max(5),
  comment: yup.string().nullable().max(1000),
})

const { handleSubmit, resetForm, isSubmitting: submitting } = useForm({
  validationSchema: schema,
  initialValues: {
    rating: 5,
    responseTimeRating: null,
    resolutionQualityRating: null,
    communicationRating: null,
    comment: '',
  },
})

const { value: rating, errorMessage: ratingError } = useField<number>('rating')
const { value: responseTimeRating, errorMessage: responseTimeRatingError } = useField<number | null>(
  'responseTimeRating'
)
const { value: resolutionQualityRating, errorMessage: resolutionQualityRatingError } = useField<number | null>(
  'resolutionQualityRating'
)
const { value: communicationRating, errorMessage: communicationRatingError } = useField<number | null>(
  'communicationRating'
)
const { value: comment, errorMessage: commentError } = useField<string | null>('comment')

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
    const response = await api.get<PagedResult<ClosedTicketSatisfactionDto>>('/company/tickets/satisfaction', {
      params,
    })
    tickets.value = response.data.items
    totalCount.value = response.data.totalCount
  } catch (error) {
    notify.notify('Failed to load satisfaction data.', 'error')
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  page.value = 1
  void loadTickets()
}

const openDialog = (ticket: ClosedTicketSatisfactionDto) => {
  selectedTicket.value = ticket
  resetForm({
    values: {
      rating: 5,
      responseTimeRating: null,
      resolutionQualityRating: null,
      communicationRating: null,
      comment: '',
    },
  })
  dialogOpen.value = true
}

const submitSatisfaction = handleSubmit(async (values) => {
  if (!selectedTicket.value) {
    return
  }
  try {
    await api.post(`/company/tickets/${selectedTicket.value.ticketId}/satisfaction`, {
      rating: values.rating,
      responseTimeRating: values.responseTimeRating,
      resolutionQualityRating: values.resolutionQualityRating,
      communicationRating: values.communicationRating,
      comment: values.comment?.trim() || null,
    })
    notify.notify('Satisfaction submitted.', 'success')
    dialogOpen.value = false
    await loadTickets()
  } catch (error) {
    notify.notify('Failed to submit satisfaction.', 'error')
  }
})

onMounted(() => {
  void loadTickets()
})
</script>
