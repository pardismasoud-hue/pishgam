<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">Experts</h2>
        <div class="text-body-2 text-medium-emphasis">Approve and manage expert accounts.</div>
      </div>
      <v-btn variant="tonal" @click="loadExperts" :loading="loading">
        Refresh
      </v-btn>
    </div>

    <v-card class="mb-4" rounded="lg" elevation="2">
      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="6">
            <v-text-field
              v-model="search"
              label="Search by name or email"
              clearable
              @keyup.enter="applyFilters"
            />
          </v-col>
          <v-col cols="12" md="4">
            <v-select
              v-model="statusFilter"
              :items="statusOptions"
              label="Approval status"
            />
          </v-col>
          <v-col cols="12" md="2" class="d-flex align-end">
            <v-btn block color="primary" @click="applyFilters">Apply</v-btn>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-card rounded="lg" elevation="2">
      <v-table>
        <thead>
          <tr>
            <th class="text-left">Name</th>
            <th class="text-left">Email</th>
            <th class="text-left">Status</th>
            <th class="text-left">Created (UTC)</th>
            <th class="text-left">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!loading && experts.length === 0">
            <td colspan="5" class="text-center py-6">No experts found.</td>
          </tr>
          <tr v-for="expert in experts" :key="expert.userId">
            <td>{{ expert.fullName }}</td>
            <td>{{ expert.email }}</td>
            <td>
              <v-chip :color="expert.isApproved ? 'success' : 'warning'" size="small" variant="tonal">
                {{ expert.isApproved ? 'Approved' : 'Pending' }}
              </v-chip>
            </td>
            <td>{{ formatUtc(expert.createdAtUtc) }}</td>
            <td>
              <v-btn
                v-if="!expert.isApproved"
                size="small"
                color="primary"
                variant="outlined"
                :loading="approvingId === expert.userId"
                @click="approveExpert(expert)"
              >
                Approve
              </v-btn>
              <span v-else class="text-medium-emphasis">-</span>
            </td>
          </tr>
        </tbody>
      </v-table>
      <v-divider />
      <v-card-actions class="d-flex justify-space-between align-center">
        <div class="text-body-2 text-medium-emphasis">
          Total: {{ totalCount }}
        </div>
        <v-pagination v-model="page" :length="totalPages" @update:model-value="loadExperts" />
      </v-card-actions>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../../plugins/axios'
import { useNotifyStore } from '../../stores/notify'

interface ExpertSummary {
  userId: string
  email: string
  fullName: string
  isApproved: boolean
  createdAtUtc: string
}

interface PagedResult<T> {
  page: number
  pageSize: number
  totalCount: number
  items: T[]
}

const notify = useNotifyStore()

const experts = ref<ExpertSummary[]>([])
const loading = ref(false)
const approvingId = ref<string | null>(null)

const search = ref('')
const statusFilter = ref<'all' | 'approved' | 'pending'>('all')
const statusOptions = [
  { title: 'All', value: 'all' },
  { title: 'Approved', value: 'approved' },
  { title: 'Pending', value: 'pending' },
]

const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

const formatUtc = (value: string) => {
  if (!value) return ''
  return new Date(value).toISOString().replace('T', ' ').replace('Z', ' UTC')
}

const loadExperts = async () => {
  loading.value = true
  try {
    const params: Record<string, string | number | boolean> = {
      page: page.value,
      pageSize: pageSize.value,
    }
    if (search.value.trim()) {
      params.search = search.value.trim()
    }
    if (statusFilter.value === 'approved') {
      params.approved = true
    } else if (statusFilter.value === 'pending') {
      params.approved = false
    }

    const response = await api.get<PagedResult<ExpertSummary>>('/admin/experts', { params })
    experts.value = response.data.items
    totalCount.value = response.data.totalCount
  } catch (error) {
    notify.notify('Failed to load experts.', 'error')
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  page.value = 1
  void loadExperts()
}

const approveExpert = async (expert: ExpertSummary) => {
  approvingId.value = expert.userId
  try {
    await api.post(`/admin/experts/${expert.userId}/approve`)
    notify.notify('Expert approved.', 'success')
    await loadExperts()
  } catch (error) {
    notify.notify('Failed to approve expert.', 'error')
  } finally {
    approvingId.value = null
  }
}

onMounted(() => {
  void loadExperts()
})
</script>
