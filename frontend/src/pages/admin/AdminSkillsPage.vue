<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">Skills</h2>
        <div class="text-body-2 text-medium-emphasis">Maintain the skill catalog for experts.</div>
      </div>
      <v-btn color="primary" @click="openCreate">New Skill</v-btn>
    </div>

    <v-card class="mb-4" rounded="lg" elevation="2">
      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="8">
            <v-text-field
              v-model="search"
              label="Search skills"
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
            <th class="text-left">Name</th>
            <th class="text-left">Description</th>
            <th class="text-left">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!loading && skills.length === 0">
            <td colspan="3" class="text-center py-6">No skills found.</td>
          </tr>
          <tr v-for="skill in skills" :key="skill.id">
            <td>{{ skill.name }}</td>
            <td class="text-medium-emphasis">{{ skill.description || '-' }}</td>
            <td>
              <v-btn size="small" variant="text" @click="openEdit(skill)">Edit</v-btn>
              <v-btn size="small" variant="text" color="error" @click="openDelete(skill)">Delete</v-btn>
            </td>
          </tr>
        </tbody>
      </v-table>
      <v-divider />
      <v-card-actions class="d-flex justify-space-between align-center">
        <div class="text-body-2 text-medium-emphasis">
          Total: {{ totalCount }}
        </div>
        <v-pagination v-model="page" :length="totalPages" @update:model-value="loadSkills" />
      </v-card-actions>
    </v-card>

    <v-dialog v-model="dialogOpen" max-width="560">
      <v-card>
        <v-card-title>{{ isEdit ? 'Edit Skill' : 'Create Skill' }}</v-card-title>
        <v-card-text>
          <v-form @submit.prevent="submitSkill">
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
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="closeDialog">Cancel</v-btn>
          <v-btn color="primary" :loading="isSubmitting" @click="submitSkill">
            {{ isEdit ? 'Save' : 'Create' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="deleteOpen" max-width="420">
      <v-card>
        <v-card-title>Delete Skill</v-card-title>
        <v-card-text>
          Are you sure you want to delete <strong>{{ deletingSkill?.name }}</strong>?
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

interface SkillItem {
  id: string
  name: string
  description?: string | null
}

interface PagedResult<T> {
  page: number
  pageSize: number
  totalCount: number
  items: T[]
}

const notify = useNotifyStore()

const skills = ref<SkillItem[]>([])
const loading = ref(false)
const search = ref('')
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

const dialogOpen = ref(false)
const isEdit = ref(false)
const editingId = ref<string | null>(null)

const deleteOpen = ref(false)
const deleting = ref(false)
const deletingSkill = ref<SkillItem | null>(null)

const schema = yup.object({
  name: yup.string().required().max(200),
  description: yup.string().max(1000).nullable(),
})

const { handleSubmit, resetForm, setValues, isSubmitting } = useForm({
  validationSchema: schema,
  initialValues: {
    name: '',
    description: '',
  },
})

const { value: name, errorMessage: nameError } = useField<string>('name')
const { value: description, errorMessage: descriptionError } = useField<string>('description')

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

const loadSkills = async () => {
  loading.value = true
  try {
    const params: Record<string, string | number> = {
      page: page.value,
      pageSize: pageSize.value,
    }
    if (search.value.trim()) {
      params.search = search.value.trim()
    }
    const response = await api.get<PagedResult<SkillItem>>('/admin/skills', { params })
    skills.value = response.data.items
    totalCount.value = response.data.totalCount
  } catch (error) {
    notify.notify('Failed to load skills.', 'error')
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  page.value = 1
  void loadSkills()
}

const openCreate = () => {
  isEdit.value = false
  editingId.value = null
  resetForm()
  dialogOpen.value = true
}

const openEdit = (skill: SkillItem) => {
  isEdit.value = true
  editingId.value = skill.id
  setValues({
    name: skill.name,
    description: skill.description ?? '',
  })
  dialogOpen.value = true
}

const closeDialog = () => {
  dialogOpen.value = false
}

const submitSkill = handleSubmit(async (values) => {
  try {
    if (isEdit.value && editingId.value) {
      await api.put(`/admin/skills/${editingId.value}`, values)
      notify.notify('Skill updated.', 'success')
    } else {
      await api.post('/admin/skills', values)
      notify.notify('Skill created.', 'success')
    }
    dialogOpen.value = false
    await loadSkills()
  } catch (error) {
    notify.notify('Failed to save skill.', 'error')
  }
})

const openDelete = (skill: SkillItem) => {
  deletingSkill.value = skill
  deleteOpen.value = true
}

const confirmDelete = async () => {
  if (!deletingSkill.value) {
    return
  }
  deleting.value = true
  try {
    await api.delete(`/admin/skills/${deletingSkill.value.id}`)
    notify.notify('Skill deleted.', 'success')
    deleteOpen.value = false
    await loadSkills()
  } catch (error) {
    notify.notify('Failed to delete skill.', 'error')
  } finally {
    deleting.value = false
  }
}

onMounted(() => {
  void loadSkills()
})
</script>
