<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">Company Assets</h2>
        <div class="text-body-2 text-medium-emphasis">View assets registered by each company.</div>
      </div>
      <div class="d-flex align-center ga-2">
        <v-btn
          color="primary"
          :disabled="!selectedCompanyId"
          @click="openCreate"
        >
          New Asset
        </v-btn>
        <v-btn variant="tonal" @click="loadCompanies" :loading="loadingCompanies">
          Refresh Companies
        </v-btn>
      </div>
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
              clearable
            />
          </v-col>
          <v-col cols="12" md="4" class="d-flex align-end">
            <v-btn block color="primary" :disabled="!selectedCompanyId" @click="loadAssets">
              Load Assets
            </v-btn>
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
            <v-btn block color="primary" variant="tonal" :disabled="!selectedCompanyId" @click="applyFilters">
              Apply
            </v-btn>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-card rounded="lg" elevation="2">
      <v-table>
        <thead>
          <tr>
            <th class="text-left">Asset</th>
            <th class="text-left">Asset Tag</th>
            <th class="text-left">Serial</th>
            <th class="text-left">Notes</th>
            <th class="text-left">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!selectedCompanyId">
            <td colspan="5" class="text-center py-6">Select a company to view assets.</td>
          </tr>
          <tr v-else-if="!loading && assets.length === 0">
            <td colspan="5" class="text-center py-6">No assets found.</td>
          </tr>
          <tr v-for="asset in assets" :key="asset.id">
            <td>
              <div class="font-weight-medium">{{ asset.name }}</div>
              <div class="text-body-2 text-medium-emphasis">{{ asset.companyName || '-' }}</div>
            </td>
            <td>{{ asset.assetTag || '-' }}</td>
            <td>{{ asset.serialNumber || '-' }}</td>
            <td class="text-medium-emphasis">{{ asset.notes || '-' }}</td>
            <td>
              <v-btn size="small" variant="text" @click="openEdit(asset)">Edit</v-btn>
              <v-btn size="small" variant="text" color="error" @click="openDelete(asset)">
                Delete
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
        <v-pagination
          v-model="page"
          :length="totalPages"
          @update:model-value="loadAssets"
        />
      </v-card-actions>
    </v-card>

    <v-dialog v-model="dialogOpen" max-width="640">
      <v-card>
        <v-card-title>{{ isEdit ? 'Edit Asset' : 'Create Asset' }}</v-card-title>
        <v-card-text>
          <v-form @submit.prevent="submitAsset">
            <v-text-field
              v-model="name"
              label="Name"
              :error-messages="nameError"
              required
            />
            <v-row dense>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="assetTag"
                  label="Asset tag"
                  :error-messages="assetTagError"
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model="serialNumber"
                  label="Serial number"
                  :error-messages="serialNumberError"
                />
              </v-col>
            </v-row>
            <v-textarea
              v-model="notes"
              label="Notes"
              :error-messages="notesError"
              rows="3"
            />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="closeDialog">Cancel</v-btn>
          <v-btn color="primary" :loading="isSubmitting" @click="submitAsset">
            {{ isEdit ? 'Save' : 'Create' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="deleteOpen" max-width="420">
      <v-card>
        <v-card-title>Delete Asset</v-card-title>
        <v-card-text>
          Are you sure you want to delete <strong>{{ deletingAsset?.name }}</strong>?
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="deleteOpen = false">Cancel</v-btn>
          <v-btn color="error" :loading="deleting" @click="confirmDelete">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useField, useForm } from 'vee-validate'
import * as yup from 'yup'
import { api } from '../../plugins/axios'
import { useNotifyStore } from '../../stores/notify'

interface CompanyOption {
  title: string
  value: string
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
const search = ref('')
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const assets = ref<AssetItem[]>([])
const loading = ref(false)

const dialogOpen = ref(false)
const isEdit = ref(false)
const editingId = ref<string | null>(null)

const deleteOpen = ref(false)
const deleting = ref(false)
const deletingAsset = ref<AssetItem | null>(null)

const schema = yup.object({
  name: yup.string().required().max(200),
  assetTag: yup.string().max(100).nullable(),
  serialNumber: yup.string().max(100).nullable(),
  notes: yup.string().max(1000).nullable(),
})

const { handleSubmit, resetForm, setValues, isSubmitting } = useForm({
  validationSchema: schema,
  initialValues: {
    name: '',
    assetTag: '',
    serialNumber: '',
    notes: '',
  },
})

const { value: name, errorMessage: nameError } = useField<string>('name')
const { value: assetTag, errorMessage: assetTagError } = useField<string>('assetTag')
const { value: serialNumber, errorMessage: serialNumberError } = useField<string>('serialNumber')
const { value: notes, errorMessage: notesError } = useField<string>('notes')

const companyOptions = computed<CompanyOption[]>(() =>
  companies.value.map((company) => ({
    title: `${company.companyName} (${company.email})`,
    value: company.userId,
  }))
)

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

const loadCompanies = async () => {
  loadingCompanies.value = true
  try {
    const response = await api.get<PagedResult<CompanySummary>>('/admin/companies', {
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
  if (!selectedCompanyId.value) {
    return
  }
  loading.value = true
  try {
    const params: Record<string, string | number> = {
      page: page.value,
      pageSize: pageSize.value,
    }
    if (search.value.trim()) {
      params.search = search.value.trim()
    }
    const response = await api.get<PagedResult<AssetItem>>(
      `/admin/companies/${selectedCompanyId.value}/assets`,
      { params }
    )
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

const openCreate = () => {
  if (!selectedCompanyId.value) {
    notify.notify('Select a company first.', 'error')
    return
  }
  isEdit.value = false
  editingId.value = null
  resetForm()
  dialogOpen.value = true
}

const openEdit = (asset: AssetItem) => {
  isEdit.value = true
  editingId.value = asset.id
  setValues({
    name: asset.name,
    assetTag: asset.assetTag ?? '',
    serialNumber: asset.serialNumber ?? '',
    notes: asset.notes ?? '',
  })
  dialogOpen.value = true
}

const closeDialog = () => {
  dialogOpen.value = false
}

const submitAsset = handleSubmit(async (values) => {
  if (!selectedCompanyId.value) {
    notify.notify('Select a company first.', 'error')
    return
  }

  const payload = {
    name: values.name,
    assetTag: values.assetTag || null,
    serialNumber: values.serialNumber || null,
    notes: values.notes || null,
  }

  try {
    if (isEdit.value && editingId.value) {
      await api.put(
        `/admin/companies/${selectedCompanyId.value}/assets/${editingId.value}`,
        payload
      )
      notify.notify('Asset updated.', 'success')
    } else {
      await api.post(`/admin/companies/${selectedCompanyId.value}/assets`, payload)
      notify.notify('Asset created.', 'success')
    }
    dialogOpen.value = false
    await loadAssets()
  } catch (error) {
    notify.notify('Failed to save asset.', 'error')
  }
})

const openDelete = (asset: AssetItem) => {
  deletingAsset.value = asset
  deleteOpen.value = true
}

const confirmDelete = async () => {
  if (!deletingAsset.value || !selectedCompanyId.value) {
    return
  }
  deleting.value = true
  try {
    await api.delete(
      `/admin/companies/${selectedCompanyId.value}/assets/${deletingAsset.value.id}`
    )
    notify.notify('Asset deleted.', 'success')
    deleteOpen.value = false
    await loadAssets()
  } catch (error) {
    notify.notify('Failed to delete asset.', 'error')
  } finally {
    deleting.value = false
  }
}

onMounted(() => {
  void loadCompanies()
})
</script>
