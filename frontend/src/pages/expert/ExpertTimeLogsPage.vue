<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">Time Logs</h2>
        <div class="text-body-2 text-medium-emphasis">Review and log support time per ticket.</div>
      </div>
      <v-btn color="primary" variant="tonal" :loading="loading" @click="refresh">
        Refresh
      </v-btn>
    </div>

    <v-card class="mb-4" rounded="lg" elevation="2">
      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="8">
            <v-select
              v-model="selectedTicketId"
              :items="ticketOptions"
              item-title="title"
              item-value="value"
              label="Ticket"
              clearable
            />
          </v-col>
          <v-col cols="12" md="4" class="d-flex align-end">
            <v-btn block color="primary" variant="tonal" :disabled="!selectedTicketId" @click="loadLogs">
              Load Logs
            </v-btn>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-card rounded="lg" elevation="2" class="mb-4">
      <v-table>
        <thead>
          <tr>
            <th class="text-left">Minutes</th>
            <th class="text-left">Work Type</th>
            <th class="text-left">Logged At (UTC)</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!loading && timeLogs.length === 0">
            <td colspan="3" class="text-center py-6">
              {{ selectedTicketId ? 'No time logs found.' : 'Select a ticket to view logs.' }}
            </td>
          </tr>
          <tr v-for="log in timeLogs" :key="log.id">
            <td>{{ log.minutes }}</td>
            <td>{{ workTypeLabel(log.workType) }}</td>
            <td>{{ formatUtc(log.loggedAtUtc) }}</td>
          </tr>
        </tbody>
      </v-table>
    </v-card>

    <v-card rounded="lg" elevation="2">
      <v-card-title>Add Time Log</v-card-title>
      <v-card-text>
        <v-form @submit.prevent="submitTimeLog">
          <v-row dense>
            <v-col cols="12" md="4">
              <v-text-field
                v-model.number="minutes"
                label="Minutes"
                type="number"
                :error-messages="minutesError"
                :disabled="!selectedTicketId"
                required
              />
            </v-col>
            <v-col cols="12" md="4">
              <v-select
                v-model="workType"
                :items="workTypeOptions"
                item-title="title"
                item-value="value"
                label="Work type"
                :error-messages="workTypeError"
                :disabled="!selectedTicketId"
                required
              />
            </v-col>
            <v-col cols="12" md="4" class="d-flex align-end">
              <v-btn
                color="primary"
                :loading="submitting"
                :disabled="!selectedTicketId"
                @click="submitTimeLog"
              >
                Add Log
              </v-btn>
            </v-col>
          </v-row>
          <div v-if="!selectedTicketId" class="text-body-2 text-medium-emphasis">
            Select a ticket to add logs.
          </div>
        </v-form>
      </v-card-text>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useForm, useField } from 'vee-validate'
import * as yup from 'yup'
import { api } from '../../plugins/axios'
import { useNotifyStore } from '../../stores/notify'

interface TicketDto {
  id: string
  title: string
  companyName: string
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
const loading = ref(false)
const tickets = ref<TicketDto[]>([])
const timeLogs = ref<TicketTimeLogDto[]>([])
const selectedTicketId = ref<string | null>(null)

const workTypeOptions = [
  { title: 'Remote', value: 1 },
  { title: 'Onsite', value: 2 },
  { title: 'On Call', value: 3 },
]

const schema = yup.object({
  minutes: yup.number().required().min(1).max(100000),
  workType: yup.number().required().oneOf([1, 2, 3]),
})

const { handleSubmit, resetForm, isSubmitting: submitting } = useForm({
  validationSchema: schema,
  initialValues: {
    minutes: 30,
    workType: 1,
  },
})

const { value: minutes, errorMessage: minutesError } = useField<number>('minutes')
const { value: workType, errorMessage: workTypeError } = useField<number>('workType')

const ticketOptions = computed(() =>
  tickets.value.map((ticket) => ({
    title: `${ticket.title} (${ticket.companyName})`,
    value: ticket.id,
  }))
)

const formatUtc = (value?: string | null) => {
  if (!value) return '-'
  return new Date(value).toISOString().replace('T', ' ').replace('Z', ' UTC')
}

const workTypeLabel = (value: number) => {
  const match = workTypeOptions.find((option) => option.value === value)
  return match?.title ?? 'Unknown'
}

const loadTickets = async () => {
  loading.value = true
  try {
    const response = await api.get<PagedResult<TicketDto>>('/expert/tickets', {
      params: { page: 1, pageSize: 200 },
    })
    tickets.value = response.data.items
  } catch (error) {
    notify.notify('Failed to load tickets.', 'error')
  } finally {
    loading.value = false
  }
}

const loadLogs = async () => {
  if (!selectedTicketId.value) {
    timeLogs.value = []
    return
  }
  loading.value = true
  try {
    const response = await api.get<TicketTimeLogDto[]>(
      `/expert/tickets/${selectedTicketId.value}/timelogs`
    )
    timeLogs.value = response.data
  } catch (error) {
    notify.notify('Failed to load time logs.', 'error')
  } finally {
    loading.value = false
  }
}

const refresh = async () => {
  await loadTickets()
  if (selectedTicketId.value) {
    await loadLogs()
  }
}

const submitTimeLog = handleSubmit(async (values) => {
  if (!selectedTicketId.value) {
    return
  }
  try {
    await api.post(`/expert/tickets/${selectedTicketId.value}/timelogs`, {
      minutes: values.minutes,
      workType: values.workType,
    })
    notify.notify('Time log added.', 'success')
    resetForm({ values: { minutes: 30, workType: values.workType } })
    await loadLogs()
  } catch (error) {
    notify.notify('Failed to add time log.', 'error')
  }
})

watch(selectedTicketId, () => {
  void loadLogs()
})

onMounted(() => {
  void loadTickets()
})
</script>
