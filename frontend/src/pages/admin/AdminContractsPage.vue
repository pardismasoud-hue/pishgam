<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">Contracts</h2>
        <div class="text-body-2 text-medium-emphasis">Create and manage company support agreements.</div>
      </div>
      <v-btn color="primary" @click="openCreate">New Contract</v-btn>
    </div>

    <v-card class="mb-4" rounded="lg" elevation="2">
      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="6">
            <v-select
              v-model="companyFilterUserId"
              :items="companyOptions"
              item-title="title"
              item-value="value"
              label="Company"
              clearable
            />
          </v-col>
          <v-col cols="12" md="4">
            <v-select
              v-model="statusFilter"
              :items="statusOptions"
              label="Status"
            />
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
            <th class="text-left">Company</th>
            <th class="text-left">Price</th>
            <th class="text-left">Included Minutes</th>
            <th class="text-left">Onsite Days</th>
            <th class="text-left">Status</th>
            <th class="text-left">Services</th>
            <th class="text-left">Assets</th>
            <th class="text-left">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!loading && contracts.length === 0">
            <td colspan="8" class="text-center py-6">No contracts found.</td>
          </tr>
          <tr v-for="contract in contracts" :key="contract.id">
            <td>{{ contract.companyName }}</td>
            <td>{{ formatCurrency(contract.monthlyPrice) }}</td>
            <td>{{ contract.includedSupportMinutesPerMonth }}</td>
            <td>{{ contract.onsiteDaysIncluded }}</td>
            <td>
              <v-chip :color="contract.isActive ? 'success' : 'warning'" size="small" variant="tonal">
                {{ contract.isActive ? 'Active' : 'Inactive' }}
              </v-chip>
            </td>
            <td>{{ contract.services.length }}</td>
            <td>{{ contract.assets.length }}</td>
            <td>
              <v-btn size="small" variant="text" @click="openDetails(contract)">View</v-btn>
              <v-btn size="small" variant="text" @click="openEdit(contract)">Edit</v-btn>
              <v-btn
                v-if="!contract.isActive"
                size="small"
                variant="text"
                color="primary"
                :loading="activatingId === contract.id"
                @click="activateContract(contract)"
              >
                Activate
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
        <v-pagination v-model="page" :length="totalPages" @update:model-value="loadContracts" />
      </v-card-actions>
    </v-card>

    <v-dialog v-model="dialogOpen" max-width="980">
      <v-card>
        <v-card-title>{{ isEdit ? 'Edit Contract' : 'Create Contract' }}</v-card-title>
        <v-card-text>
          <v-form @submit.prevent="submitContract">
            <v-row dense>
              <v-col cols="12" md="6">
                <v-select
                  v-model="companyUserId"
                  :items="companyOptions"
                  item-title="title"
                  item-value="value"
                  label="Company"
                  :disabled="isEdit"
                  :error-messages="companyError"
                  required
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-switch v-model="isActive" label="Active contract" inset />
              </v-col>
            </v-row>
            <v-row dense>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="monthlyPrice"
                  label="Monthly price"
                  type="number"
                  :error-messages="monthlyPriceError"
                  required
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="includedSupportMinutesPerMonth"
                  label="Included support minutes"
                  type="number"
                  :error-messages="includedMinutesError"
                  required
                />
              </v-col>
              <v-col cols="12" md="4">
                <v-text-field
                  v-model.number="onsiteDaysIncluded"
                  label="Onsite days included"
                  type="number"
                  :error-messages="onsiteDaysError"
                  required
                />
              </v-col>
            </v-row>

            <div class="text-subtitle-1 font-weight-medium mt-4 mb-2">Services</div>
            <v-card variant="outlined" rounded="lg" class="mb-4">
              <v-table>
                <thead>
                  <tr>
                    <th class="text-left">Select</th>
                    <th class="text-left">Service</th>
                    <th class="text-left">Default SLA</th>
                    <th class="text-left">Custom First Response</th>
                    <th class="text-left">Custom Resolution</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="service in serviceSelections" :key="service.serviceId">
                    <td>
                      <v-checkbox v-model="service.selected" hide-details />
                    </td>
                    <td>
                      <div class="font-weight-medium">{{ service.name }}</div>
                      <div class="text-body-2 text-medium-emphasis">{{ service.description || '-' }}</div>
                    </td>
                    <td>
                      {{ service.defaultFirstResponseMinutes }} / {{ service.defaultResolutionMinutes }} min
                    </td>
                    <td>
                      <v-text-field
                        v-model.number="service.customFirstResponseMinutes"
                        type="number"
                        :disabled="!service.selected"
                        hide-details
                        density="compact"
                      />
                    </td>
                    <td>
                      <v-text-field
                        v-model.number="service.customResolutionMinutes"
                        type="number"
                        :disabled="!service.selected"
                        hide-details
                        density="compact"
                      />
                    </td>
                  </tr>
                </tbody>
              </v-table>
            </v-card>

            <div class="text-subtitle-1 font-weight-medium mt-4 mb-2">Assets</div>
            <v-card variant="outlined" rounded="lg">
              <v-card-text>
                <v-row dense>
                  <v-col cols="12" md="8">
                    <v-text-field
                      v-model="assetSearch"
                      label="Search assets"
                      clearable
                      @keyup.enter="loadAssets"
                    />
                  </v-col>
                  <v-col cols="12" md="4" class="d-flex align-end">
                    <v-btn block variant="tonal" @click="loadAssets" :disabled="!companyUserId">
                      Refresh Assets
                    </v-btn>
                  </v-col>
                </v-row>
                <v-divider class="my-3" />
                <v-row dense>
                  <v-col cols="12" md="6" v-for="asset in assets" :key="asset.id">
                    <v-checkbox
                      v-model="selectedAssetIds"
                      :value="asset.id"
                      :label="formatAssetLabel(asset)"
                      hide-details
                    />
                  </v-col>
                </v-row>
              </v-card-text>
            </v-card>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="closeDialog">Cancel</v-btn>
          <v-btn color="primary" :loading="isSubmitting" @click="submitContract">
            {{ isEdit ? 'Save' : 'Create' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="detailsOpen" max-width="720">
      <v-card>
        <v-card-title>Contract Details</v-card-title>
        <v-card-text v-if="selectedContract">
          <div class="text-subtitle-1 font-weight-medium mb-2">Services</div>
          <v-list density="compact">
            <v-list-item v-for="service in selectedContract.services" :key="service.serviceId">
              <v-list-item-title>{{ service.serviceName }}</v-list-item-title>
              <v-list-item-subtitle>
                Default {{ service.defaultFirstResponseMinutes }}/{{ service.defaultResolutionMinutes }} min
                - Custom
                {{ service.customFirstResponseMinutes ?? '-' }}/{{ service.customResolutionMinutes ?? '-' }} min
              </v-list-item-subtitle>
            </v-list-item>
            <v-list-item v-if="selectedContract.services.length === 0">
              <v-list-item-title>No services assigned.</v-list-item-title>
            </v-list-item>
          </v-list>

          <div class="text-subtitle-1 font-weight-medium mt-4 mb-2">Assets</div>
          <v-list density="compact">
            <v-list-item v-for="asset in selectedContract.assets" :key="asset.assetId">
              <v-list-item-title>{{ asset.assetName }}</v-list-item-title>
              <v-list-item-subtitle>
                {{ asset.assetTag || '-' }} {{ asset.serialNumber || '-' }}
              </v-list-item-subtitle>
            </v-list-item>
            <v-list-item v-if="selectedContract.assets.length === 0">
              <v-list-item-title>No assets assigned.</v-list-item-title>
            </v-list-item>
          </v-list>
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
import { useForm, useField } from 'vee-validate'
import * as yup from 'yup'
import { api } from '../../plugins/axios'
import { useNotifyStore } from '../../stores/notify'

interface CompanySummary {
  userId: string
  companyProfileId: string
  email: string
  companyName: string
}

interface CompanyOption {
  title: string
  value: string
}

interface ServiceItem {
  id: string
  name: string
  description?: string | null
  defaultFirstResponseMinutes: number
  defaultResolutionMinutes: number
}

interface ServiceSelection extends ServiceItem {
  serviceId: string
  selected: boolean
  customFirstResponseMinutes: number | null
  customResolutionMinutes: number | null
}

interface AssetItem {
  id: string
  name: string
  assetTag?: string | null
  serialNumber?: string | null
}

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
const activatingId = ref<string | null>(null)

const companies = ref<CompanySummary[]>([])
const companyFilterUserId = ref<string | null>(null)
const statusFilter = ref<'all' | 'active' | 'inactive'>('all')
const statusOptions = [
  { title: 'All', value: 'all' },
  { title: 'Active', value: 'active' },
  { title: 'Inactive', value: 'inactive' },
]

const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

const dialogOpen = ref(false)
const isEdit = ref(false)
const editingId = ref<string | null>(null)

const detailsOpen = ref(false)
const selectedContract = ref<ContractDto | null>(null)

const services = ref<ServiceItem[]>([])
const serviceSelections = ref<ServiceSelection[]>([])

const assets = ref<AssetItem[]>([])
const selectedAssetIds = ref<string[]>([])
const assetSearch = ref('')

const schema = yup.object({
  companyUserId: yup.string().required(),
  monthlyPrice: yup.number().required().min(0),
  includedSupportMinutesPerMonth: yup.number().required().min(0),
  onsiteDaysIncluded: yup.number().required().min(0),
  isActive: yup.boolean().required(),
})

const { handleSubmit, resetForm, setValues, isSubmitting } = useForm({
  validationSchema: schema,
  initialValues: {
    companyUserId: '',
    monthlyPrice: 0,
    includedSupportMinutesPerMonth: 0,
    onsiteDaysIncluded: 0,
    isActive: false,
  },
})

const { value: companyUserId, errorMessage: companyError } = useField<string>('companyUserId')
const { value: monthlyPrice, errorMessage: monthlyPriceError } = useField<number>('monthlyPrice')
const { value: includedSupportMinutesPerMonth, errorMessage: includedMinutesError } =
  useField<number>('includedSupportMinutesPerMonth')
const { value: onsiteDaysIncluded, errorMessage: onsiteDaysError } =
  useField<number>('onsiteDaysIncluded')
const { value: isActive } = useField<boolean>('isActive')

const companyOptions = computed<CompanyOption[]>(() =>
  companies.value.map((company) => ({
    title: `${company.companyName} (${company.email})`,
    value: company.userId,
  }))
)

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)

const formatAssetLabel = (asset: AssetItem) =>
  `${asset.name} (${asset.assetTag || '-'}, ${asset.serialNumber || '-'})`

const loadCompanies = async () => {
  try {
    const response = await api.get<PagedResult<CompanySummary>>('/admin/companies', {
      params: { page: 1, pageSize: 200 },
    })
    companies.value = response.data.items
  } catch (error) {
    notify.notify('Failed to load companies.', 'error')
  }
}

const loadServices = async () => {
  try {
    const response = await api.get<PagedResult<ServiceItem>>('/admin/services', {
      params: { page: 1, pageSize: 200 },
    })
    services.value = response.data.items
    serviceSelections.value = response.data.items.map((service) => ({
      ...service,
      serviceId: service.id,
      selected: false,
      customFirstResponseMinutes: null,
      customResolutionMinutes: null,
    }))
  } catch (error) {
    notify.notify('Failed to load services.', 'error')
  }
}

const loadContracts = async () => {
  loading.value = true
  try {
    const params: Record<string, string | number | boolean> = {
      page: page.value,
      pageSize: pageSize.value,
    }
    if (companyFilterUserId.value) {
      params.companyId = companyFilterUserId.value
    }
    if (statusFilter.value === 'active') {
      params.isActive = true
    } else if (statusFilter.value === 'inactive') {
      params.isActive = false
    }
    const response = await api.get<PagedResult<ContractDto>>('/admin/contracts', { params })
    contracts.value = response.data.items
    totalCount.value = response.data.totalCount
  } catch (error) {
    notify.notify('Failed to load contracts.', 'error')
  } finally {
    loading.value = false
  }
}

const loadAssets = async () => {
  if (!companyUserId.value) {
    assets.value = []
    selectedAssetIds.value = []
    return
  }
  try {
    const params: Record<string, string | number> = {
      page: 1,
      pageSize: 200,
    }
    if (assetSearch.value.trim()) {
      params.search = assetSearch.value.trim()
    }
    const response = await api.get<PagedResult<AssetItem>>(
      `/admin/companies/${companyUserId.value}/assets`,
      { params }
    )
    assets.value = response.data.items
  } catch (error) {
    notify.notify('Failed to load assets.', 'error')
  }
}

const applyFilters = () => {
  page.value = 1
  void loadContracts()
}

const openCreate = () => {
  isEdit.value = false
  editingId.value = null
  resetForm({
    values: {
      companyUserId: '',
      monthlyPrice: 0,
      includedSupportMinutesPerMonth: 0,
      onsiteDaysIncluded: 0,
      isActive: false,
    },
  })
  selectedAssetIds.value = []
  serviceSelections.value = services.value.map((service) => ({
    ...service,
    serviceId: service.id,
    selected: false,
    customFirstResponseMinutes: null,
    customResolutionMinutes: null,
  }))
  dialogOpen.value = true
}

const openEdit = async (contract: ContractDto) => {
  isEdit.value = true
  editingId.value = contract.id
  const company = companies.value.find((c) => c.companyProfileId === contract.companyProfileId)
  if (!company) {
    notify.notify('Unable to locate company for this contract.', 'error')
    return
  }
  resetForm({
    values: {
      companyUserId: company.userId,
      monthlyPrice: contract.monthlyPrice,
      includedSupportMinutesPerMonth: contract.includedSupportMinutesPerMonth,
      onsiteDaysIncluded: contract.onsiteDaysIncluded,
      isActive: contract.isActive,
    },
  })
  await loadAssets()
  selectedAssetIds.value = contract.assets.map((asset) => asset.assetId)
  serviceSelections.value = services.value.map((service) => {
    const selected = contract.services.find((s) => s.serviceId === service.id)
    return {
      ...service,
      serviceId: service.id,
      selected: !!selected,
      customFirstResponseMinutes: selected?.customFirstResponseMinutes ?? null,
      customResolutionMinutes: selected?.customResolutionMinutes ?? null,
    }
  })
  dialogOpen.value = true
}

const openDetails = (contract: ContractDto) => {
  selectedContract.value = contract
  detailsOpen.value = true
}

const closeDialog = () => {
  dialogOpen.value = false
}

const submitContract = handleSubmit(async (values) => {
  const selectedServices = serviceSelections.value.filter((service) => service.selected)
  if (selectedServices.length === 0) {
    notify.notify('Select at least one service.', 'warning')
    return
  }

  const payload = {
    monthlyPrice: Number(values.monthlyPrice),
    includedSupportMinutesPerMonth: Number(values.includedSupportMinutesPerMonth),
    onsiteDaysIncluded: Number(values.onsiteDaysIncluded),
    isActive: values.isActive,
    services: selectedServices.map((service) => ({
      serviceId: service.serviceId,
      customFirstResponseMinutes: service.customFirstResponseMinutes || null,
      customResolutionMinutes: service.customResolutionMinutes || null,
    })),
    assetIds: selectedAssetIds.value,
  }

  try {
    if (isEdit.value && editingId.value) {
      await api.put(`/admin/contracts/${editingId.value}`, payload)
      notify.notify('Contract updated.', 'success')
    } else {
      await api.post('/admin/contracts', {
        companyUserId: values.companyUserId,
        ...payload,
      })
      notify.notify('Contract created.', 'success')
    }
    dialogOpen.value = false
    await loadContracts()
  } catch (error) {
    notify.notify('Failed to save contract.', 'error')
  }
})

const activateContract = async (contract: ContractDto) => {
  activatingId.value = contract.id
  try {
    await api.post(`/admin/contracts/${contract.id}/activate`)
    notify.notify('Contract activated.', 'success')
    await loadContracts()
  } catch (error) {
    notify.notify('Failed to activate contract.', 'error')
  } finally {
    activatingId.value = null
  }
}

watch(companyUserId, () => {
  if (!isEdit.value) {
    selectedAssetIds.value = []
  }
  void loadAssets()
})

onMounted(async () => {
  await loadCompanies()
  await loadServices()
  await loadContracts()
})
</script>
