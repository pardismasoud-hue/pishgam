<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">Contracts</h2>
        <div class="text-body-2 text-medium-emphasis">View active and historical agreements.</div>
      </div>
      <v-btn variant="tonal" @click="loadContracts" :loading="loading">Refresh</v-btn>
    </div>

    <v-card class="mb-4" rounded="lg" elevation="2">
      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="6">
            <v-select
              v-model="statusFilter"
              :items="statusOptions"
              label="Status"
            />
          </v-col>
          <v-col cols="12" md="6" class="d-flex align-end">
            <v-btn block color="primary" variant="tonal" @click="applyFilters">Apply</v-btn>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-expansion-panels variant="accordion" elevation="2">
      <v-expansion-panel v-for="contract in contracts" :key="contract.id">
        <v-expansion-panel-title>
          <div class="d-flex flex-wrap align-center justify-space-between w-100">
            <div class="font-weight-medium">
              {{ contract.companyName }}
              <span class="text-medium-emphasis ml-2">Contract</span>
            </div>
            <div class="d-flex flex-wrap align-center">
              <v-chip :color="contract.isActive ? 'success' : 'warning'" size="small" variant="tonal" class="mr-2">
                {{ contract.isActive ? 'Active' : 'Inactive' }}
              </v-chip>
              <span class="text-body-2 text-medium-emphasis mr-4">
                {{ formatCurrency(contract.monthlyPrice) }}/month
              </span>
              <span class="text-body-2 text-medium-emphasis">
                {{ contract.includedSupportMinutesPerMonth }} minutes
              </span>
            </div>
          </div>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <v-row dense class="mb-4">
            <v-col cols="12" md="4">
              <div class="text-body-2 text-medium-emphasis">Onsite days included</div>
              <div class="text-subtitle-1 font-weight-medium">{{ contract.onsiteDaysIncluded }}</div>
            </v-col>
            <v-col cols="12" md="4">
              <div class="text-body-2 text-medium-emphasis">Services</div>
              <div class="text-subtitle-1 font-weight-medium">{{ contract.services.length }}</div>
            </v-col>
            <v-col cols="12" md="4">
              <div class="text-body-2 text-medium-emphasis">Assets covered</div>
              <div class="text-subtitle-1 font-weight-medium">{{ contract.assets.length }}</div>
            </v-col>
          </v-row>

          <div class="text-subtitle-1 font-weight-medium mb-2">Services</div>
          <v-list density="compact">
            <v-list-item v-for="service in contract.services" :key="service.serviceId">
              <v-list-item-title>{{ service.serviceName }}</v-list-item-title>
              <v-list-item-subtitle>
                Default {{ service.defaultFirstResponseMinutes }}/{{ service.defaultResolutionMinutes }} min
                - Custom
                {{ service.customFirstResponseMinutes ?? '-' }}/{{ service.customResolutionMinutes ?? '-' }} min
              </v-list-item-subtitle>
            </v-list-item>
            <v-list-item v-if="contract.services.length === 0">
              <v-list-item-title>No services attached.</v-list-item-title>
            </v-list-item>
          </v-list>

          <div class="text-subtitle-1 font-weight-medium mt-4 mb-2">Assets</div>
          <v-list density="compact">
            <v-list-item v-for="asset in contract.assets" :key="asset.assetId">
              <v-list-item-title>{{ asset.assetName }}</v-list-item-title>
              <v-list-item-subtitle>
                {{ asset.assetTag || '-' }} {{ asset.serialNumber || '-' }}
              </v-list-item-subtitle>
            </v-list-item>
            <v-list-item v-if="contract.assets.length === 0">
              <v-list-item-title>No assets attached.</v-list-item-title>
            </v-list-item>
          </v-list>
        </v-expansion-panel-text>
      </v-expansion-panel>
      <v-expansion-panel v-if="!loading && contracts.length === 0">
        <v-expansion-panel-title>No contracts found.</v-expansion-panel-title>
      </v-expansion-panel>
    </v-expansion-panels>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api } from '../../plugins/axios'
import { useNotifyStore } from '../../stores/notify'

interface ContractServiceDto {
  serviceId: string
  serviceName: string
  defaultFirstResponseMinutes: number
  defaultResolutionMinutes: number
  customFirstResponseMinutes?: number | null
  customResolutionMinutes?: number | null
}

interface ContractAssetDto {
  assetId: string
  assetName: string
  assetTag?: string | null
  serialNumber?: string | null
}

interface ContractDto {
  id: string
  companyProfileId: string
  companyName: string
  monthlyPrice: number
  includedSupportMinutesPerMonth: number
  onsiteDaysIncluded: number
  isActive: boolean
  services: ContractServiceDto[]
  assets: ContractAssetDto[]
}

interface PagedResult<T> {
  page: number
  pageSize: number
  totalCount: number
  items: T[]
}

const notify = useNotifyStore()

const contracts = ref<ContractDto[]>([])
const loading = ref(false)

const statusFilter = ref<'all' | 'active' | 'inactive'>('all')
const statusOptions = [
  { title: 'All', value: 'all' },
  { title: 'Active', value: 'active' },
  { title: 'Inactive', value: 'inactive' },
]

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)

const loadContracts = async () => {
  loading.value = true
  try {
    const params: Record<string, string | number | boolean> = { page: 1, pageSize: 50 }
    if (statusFilter.value === 'active') {
      params.isActive = true
    } else if (statusFilter.value === 'inactive') {
      params.isActive = false
    }
    const response = await api.get<PagedResult<ContractDto>>('/company/contracts', { params })
    contracts.value = response.data.items
  } catch (error) {
    notify.notify('Failed to load contracts.', 'error')
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  void loadContracts()
}

onMounted(() => {
  void loadContracts()
})
</script>
