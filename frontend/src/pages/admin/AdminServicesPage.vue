<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">Services</h2>
        <div class="text-body-2 text-medium-emphasis">Manage service catalog and SLA defaults.</div>
      </div>
      <v-btn color="primary" @click="openCreate">New Service</v-btn>
    </div>

    <v-card class="mb-4" rounded="lg" elevation="2">
      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="6">
            <v-text-field
              v-model="search"
              label="Search services"
              clearable
              @keyup.enter="applyFilters"
            />
          </v-col>
          <v-col cols="12" md="4">
            <v-select
              v-model="activeFilter"
              :items="activeOptions"
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
            <th class="text-left">Name</th>
            <th class="text-left">SLA First Response</th>
            <th class="text-left">SLA Resolution</th>
            <th class="text-left">Status</th>
            <th class="text-left">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!loading && services.length === 0">
            <td colspan="5" class="text-center py-6">No services found.</td>
          </tr>
          <tr v-for="service in services" :key="service.id">
            <td>
              <div class="font-weight-medium">{{ service.name }}</div>
              <div class="text-body-2 text-medium-emphasis">{{ service.description || '-' }}</div>
            </td>
            <td>{{ service.defaultFirstResponseMinutes }} min</td>
            <td>{{ service.defaultResolutionMinutes }} min</td>
            <td>
              <v-chip :color="service.isActive ? 'success' : 'warning'" size="small" variant="tonal">
                {{ service.isActive ? 'Active' : 'Inactive' }}
              </v-chip>
            </td>
            <td>
              <v-btn size="small" variant="text" @click="openEdit(service)">Edit</v-btn>
              <v-btn size="small" variant="text" color="error" @click="openDelete(service)">Delete</v-btn>
            </td>
          </tr>
        </tbody>
      </v-table>
      <v-divider />
      <v-card-actions class="d-flex justify-space-between align-center">
        <div class="text-body-2 text-medium-emphasis">
          Total: {{ totalCount }}
        </div>
        <v-pagination v-model="page" :length="totalPages" @update:model-value="loadServices" />
      </v-card-actions>
    </v-card>

    <v-dialog v-model="dialogOpen" max-width="640">
      <v-card>
        <v-card-title>{{ isEdit ? 'Edit Service' : 'Create Service' }}</v-card-title>
        <v-card-text>
          <v-form @submit.prevent="submitService">
            <v-text-field
              v-model="name"
              label="Name"
              :error-messages="nameError"
              required
            />
            <v-textarea
              v-model="description"
              label="Description"
              :error-messages="descriptionError"
              rows="3"
            />
            <v-row dense>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model.number="defaultFirstResponseMinutes"
                  label="First response minutes"
                  type="number"
                  :error-messages="firstResponseError"
                  required
                />
              </v-col>
              <v-col cols="12" md="6">
                <v-text-field
                  v-model.number="defaultResolutionMinutes"
                  label="Resolution minutes"
                  type="number"
                  :error-messages="resolutionError"
                  required
                />
              </v-col>
            </v-row>
            <v-switch v-model="isActive" label="Active service" inset />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="closeDialog">Cancel</v-btn>
          <v-btn color="primary" :loading="isSubmitting" @click="submitService">
            {{ isEdit ? 'Save' : 'Create' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="deleteOpen" max-width="420">
      <v-card>
        <v-card-title>Delete Service</v-card-title>
        <v-card-text>
          Are you sure you want to delete <strong>{{ deletingService?.name }}</strong>?
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
import { useForm, useField } from 'vee-validate'
import * as yup from 'yup'
import { api } from '../../plugins/axios'
import { useNotifyStore } from '../../stores/notify'

interface ServiceItem {
  id: string
  name: string
  description?: string | null
  defaultFirstResponseMinutes: number
  defaultResolutionMinutes: number
  isActive: boolean
}

interface PagedResult<T> {
  page: number
  pageSize: number
  totalCount: number
  items: T[]
}

const notify = useNotifyStore()

const services = ref<ServiceItem[]>([])
const loading = ref(false)
const search = ref('')
const activeFilter = ref<'all' | 'active' | 'inactive'>('all')
const activeOptions = [
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

const deleteOpen = ref(false)
const deleting = ref(false)
const deletingService = ref<ServiceItem | null>(null)

const schema = yup.object({
  name: yup.string().required().max(200),
  description: yup.string().max(1000).nullable(),
  defaultFirstResponseMinutes: yup.number().required().min(1).max(100000),
  defaultResolutionMinutes: yup.number().required().min(1).max(100000),
  isActive: yup.boolean().required(),
})

const { handleSubmit, resetForm, setValues, isSubmitting } = useForm({
  validationSchema: schema,
  initialValues: {
    name: '',
    description: '',
    defaultFirstResponseMinutes: 60,
    defaultResolutionMinutes: 480,
    isActive: true,
  },
})

const { value: name, errorMessage: nameError } = useField<string>('name')
const { value: description, errorMessage: descriptionError } = useField<string>('description')
const { value: defaultFirstResponseMinutes, errorMessage: firstResponseError } =
  useField<number>('defaultFirstResponseMinutes')
const { value: defaultResolutionMinutes, errorMessage: resolutionError } =
  useField<number>('defaultResolutionMinutes')
const { value: isActive } = useField<boolean>('isActive')

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

const loadServices = async () => {
  loading.value = true
  try {
    const params: Record<string, string | number | boolean> = {
      page: page.value,
      pageSize: pageSize.value,
    }
    if (search.value.trim()) {
      params.search = search.value.trim()
    }
    if (activeFilter.value === 'active') {
      params.isActive = true
    } else if (activeFilter.value === 'inactive') {
      params.isActive = false
    }
    const response = await api.get<PagedResult<ServiceItem>>('/admin/services', { params })
    services.value = response.data.items
    totalCount.value = response.data.totalCount
  } catch (error) {
    notify.notify('Failed to load services.', 'error')
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  page.value = 1
  void loadServices()
}

const openCreate = () => {
  isEdit.value = false
  editingId.value = null
  resetForm({
    values: {
      name: '',
      description: '',
      defaultFirstResponseMinutes: 60,
      defaultResolutionMinutes: 480,
      isActive: true,
    },
  })
  dialogOpen.value = true
}

const openEdit = (service: ServiceItem) => {
  isEdit.value = true
  editingId.value = service.id
  setValues({
    name: service.name,
    description: service.description ?? '',
    defaultFirstResponseMinutes: service.defaultFirstResponseMinutes,
    defaultResolutionMinutes: service.defaultResolutionMinutes,
    isActive: service.isActive,
  })
  dialogOpen.value = true
}

const closeDialog = () => {
  dialogOpen.value = false
}

const submitService = handleSubmit(async (values) => {
  const payload = {
    name: values.name,
    description: values.description || null,
    defaultFirstResponseMinutes: Number(values.defaultFirstResponseMinutes),
    defaultResolutionMinutes: Number(values.defaultResolutionMinutes),
    isActive: values.isActive,
  }

  try {
    if (isEdit.value && editingId.value) {
      await api.put(`/admin/services/${editingId.value}`, payload)
      notify.notify('Service updated.', 'success')
    } else {
      await api.post('/admin/services', payload)
      notify.notify('Service created.', 'success')
    }
    dialogOpen.value = false
    await loadServices()
  } catch (error) {
    notify.notify('Failed to save service.', 'error')
  }
})

const openDelete = (service: ServiceItem) => {
  deletingService.value = service
  deleteOpen.value = true
}

const confirmDelete = async () => {
  if (!deletingService.value) {
    return
  }
  deleting.value = true
  try {
    await api.delete(`/admin/services/${deletingService.value.id}`)
    notify.notify('Service deleted.', 'success')
    deleteOpen.value = false
    await loadServices()
  } catch (error) {
    notify.notify('Failed to delete service.', 'error')
  } finally {
    deleting.value = false
  }
}

onMounted(() => {
  void loadServices()
})
</script>
