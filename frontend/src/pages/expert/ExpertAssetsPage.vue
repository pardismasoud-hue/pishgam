<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">Company Assets</h2>
        <div class="text-body-2 text-medium-emphasis">Review assets for linked companies.</div>
      </div>
      <v-btn variant="tonal" @click="loadCompanies" :loading="loadingCompanies">
        Refresh Companies
      </v-btn>
    </div>

    <v-card class="mb-4" rounded="lg" elevation="2">
      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="8">
            <v-select
              v-model="selectedCompanyId"
              :items="companyOptions"
              item-title="title"
              item-value="value"
              label="Company"
            />
          </v-col>
          <v-col cols="12" md="4" class="d-flex align-end">
            <v-btn block color="primary" @click="loadAssets">Load Assets</v-btn>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-card class="mb-4" rounded="lg" elevation="2">
      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="8">
            <v-text-field
              v-model="search"
              label="Search assets"
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
            <th class="text-left">Asset</th>
            <th class="text-left">Company</th>
            <th class="text-left">Asset Tag</th>
            <th class="text-left">Serial</th>
            <th class="text-left">Notes</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!loading && assets.length === 0">
            <td colspan="5" class="text-center py-6">No assets found.</td>
          </tr>
          <tr v-for="asset in assets" :key="asset.id">
            <td>{{ asset.name }}</td>
            <td>{{ asset.companyName || '-' }}</td>
            <td>{{ asset.assetTag || '-' }}</td>
            <td>{{ asset.serialNumber || '-' }}</td>
            <td class="text-medium-emphasis">{{ asset.notes || '-' }}</td>
          </tr>
        </tbody>
      </v-table>
      <v-divider />
      <v-card-actions class="d-flex justify-space-between align-center">
        <div class="text-body-2 text-medium-emphasis">
          Total: {{ totalCount }}
        </div>
        <v-pagination
          v-model="page"
          :length="totalPages"
          @update:model-value="loadAssets"
        />
      </v-card-actions>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api } from '../../plugins/axios'
import { useNotifyStore } from '../../stores/notify'

interface CompanyOption {
  title: string
  value: string | null
}

interface CompanySummary {
  userId: string
  email: string
  companyName: string
}

interface AssetItem {
  id: string
  companyProfileId: string
  companyName?: string | null
  name: string
  assetTag?: string | null
  serialNumber?: string | null
  notes?: string | null
}

interface PagedResult<T> {
  page: number
  pageSize: number
  totalCount: number
  items: T[]
}

const notify = useNotifyStore()

const companies = ref<CompanySummary[]>([])
const loadingCompanies = ref(false)
const selectedCompanyId = ref<string | null>(null)

const assets = ref<AssetItem[]>([])
const loading = ref(false)
const search = ref('')
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

const companyOptions = computed<CompanyOption[]>(() => [
  { title: 'All linked companies', value: null },
  ...companies.value.map((company) => ({
    title: `${company.companyName} (${company.email})`,
    value: company.userId,
  })),
])

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

const loadCompanies = async () => {
  loadingCompanies.value = true
  try {
    const response = await api.get<PagedResult<CompanySummary>>('/expert/companies', {
      params: { page: 1, pageSize: 100 },
    })
    companies.value = response.data.items
  } catch (error) {
    notify.notify('Failed to load companies.', 'error')
  } finally {
    loadingCompanies.value = false
  }
}

const loadAssets = async () => {
  loading.value = true
  try {
    const params: Record<string, string | number> = {
      page: page.value,
      pageSize: pageSize.value,
    }
    if (search.value.trim()) {
      params.search = search.value.trim()
    }
    if (selectedCompanyId.value) {
      params.companyId = selectedCompanyId.value
    }
    const response = await api.get<PagedResult<AssetItem>>('/expert/assets', { params })
    assets.value = response.data.items
    totalCount.value = response.data.totalCount
  } catch (error) {
    notify.notify('Failed to load assets.', 'error')
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  page.value = 1
  void loadAssets()
}

onMounted(() => {
  void loadCompanies()
  void loadAssets()
})
</script>
