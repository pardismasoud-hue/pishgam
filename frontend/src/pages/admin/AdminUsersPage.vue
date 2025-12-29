<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">Users and Roles</h2>
        <div class="text-body-2 text-medium-emphasis">Manage identities and access levels.</div>
      </div>
      <div class="d-flex ga-2">
        <v-btn variant="tonal" @click="loadUsers" :loading="loading">Refresh</v-btn>
        <v-btn color="primary" @click="openCreate">New User</v-btn>
      </div>
    </div>

    <v-card class="mb-4" rounded="lg" elevation="2">
      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="6">
            <v-text-field
              v-model="search"
              label="Search by email"
              clearable
              @keyup.enter="applyFilters"
            />
          </v-col>
          <v-col cols="12" md="4">
            <v-select v-model="roleFilter" :items="roleFilterOptions" label="Role" />
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
            <th class="text-left">Email</th>
            <th class="text-left">Roles</th>
            <th class="text-left">Created (UTC)</th>
            <th class="text-left">Status</th>
            <th class="text-left">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!loading && users.length === 0">
            <td colspan="5" class="text-center py-6">No users found.</td>
          </tr>
          <tr v-for="user in users" :key="user.id">
            <td>{{ user.email }}</td>
            <td>
              <v-chip
                v-for="role in user.roles"
                :key="role"
                size="x-small"
                class="mr-1"
                variant="tonal"
              >
                {{ role }}
              </v-chip>
            </td>
            <td>{{ formatUtc(user.createdAtUtc) }}</td>
            <td>
              <v-chip :color="user.isLockedOut ? 'warning' : 'success'" size="small" variant="tonal">
                {{ user.isLockedOut ? 'Locked' : 'Active' }}
              </v-chip>
            </td>
            <td>
              <v-btn size="small" variant="text" @click="openRoleDialog(user)">Change Role</v-btn>
              <v-btn
                size="small"
                variant="text"
                :loading="lockingId === user.id"
                @click="toggleLock(user)"
              >
                {{ user.isLockedOut ? 'Unlock' : 'Lock' }}
              </v-btn>
              <v-btn size="small" variant="text" @click="openResetDialog(user)">Reset Password</v-btn>
              <v-btn size="small" variant="text" color="error" @click="openDeleteDialog(user)">
                Delete
              </v-btn>
            </td>
          </tr>
        </tbody>
      </v-table>
      <v-divider />
      <v-card-actions class="d-flex justify-space-between align-center">
        <div class="text-body-2 text-medium-emphasis">Total: {{ totalCount }}</div>
        <v-pagination v-model="page" :length="totalPages" @update:model-value="loadUsers" />
      </v-card-actions>
    </v-card>

    <v-dialog v-model="createOpen" max-width="640">
      <v-card>
        <v-card-title>Create User</v-card-title>
        <v-card-text>
          <v-form @submit.prevent="submitCreate">
            <v-text-field v-model="newEmail" label="Email" :error-messages="newEmailError" required />
            <v-text-field
              v-model="newPassword"
              label="Password"
              type="password"
              :error-messages="newPasswordError"
              required
            />
            <v-select
              v-model="newRole"
              :items="roleOptions"
              label="Role"
              :error-messages="newRoleError"
              required
            />
            <v-text-field
              v-if="newRole === 'Expert'"
              v-model="newFullName"
              label="Full name"
              :error-messages="newFullNameError"
              required
            />
            <v-text-field
              v-if="newRole === 'Company'"
              v-model="newCompanyName"
              label="Company name"
              :error-messages="newCompanyNameError"
              required
            />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="createOpen = false">Cancel</v-btn>
          <v-btn color="primary" :loading="creating" @click="submitCreate">Create</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="roleOpen" max-width="520">
      <v-card>
        <v-card-title>Change Role</v-card-title>
        <v-card-text>
          <div v-if="selectedUser" class="mb-3">
            <div class="text-body-2 text-medium-emphasis">User</div>
            <div class="text-subtitle-1 font-weight-medium">{{ selectedUser.email }}</div>
          </div>
          <v-form @submit.prevent="submitRole">
            <v-select
              v-model="roleRole"
              :items="roleOptions"
              label="Role"
              :error-messages="roleRoleError"
              required
            />
            <v-text-field
              v-if="roleRole === 'Expert'"
              v-model="roleFullName"
              label="Full name"
              :error-messages="roleFullNameError"
              required
            />
            <v-text-field
              v-if="roleRole === 'Company'"
              v-model="roleCompanyName"
              label="Company name"
              :error-messages="roleCompanyNameError"
              required
            />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="roleOpen = false">Cancel</v-btn>
          <v-btn color="primary" :loading="updatingRole" @click="submitRole">Save</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="resetOpen" max-width="520">
      <v-card>
        <v-card-title>Reset Password</v-card-title>
        <v-card-text>
          <div v-if="selectedUser" class="mb-3">
            <div class="text-body-2 text-medium-emphasis">User</div>
            <div class="text-subtitle-1 font-weight-medium">{{ selectedUser.email }}</div>
          </div>
          <v-form @submit.prevent="submitReset">
            <v-text-field
              v-model="resetPassword"
              label="New password"
              type="password"
              :error-messages="resetPasswordError"
              required
            />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="resetOpen = false">Cancel</v-btn>
          <v-btn color="primary" :loading="resetting" @click="submitReset">Reset</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="deleteOpen" max-width="420">
      <v-card>
        <v-card-title>Delete User</v-card-title>
        <v-card-text>
          Are you sure you want to delete <strong>{{ selectedUser?.email }}</strong>?
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
import { computed, onMounted, ref, watch } from 'vue'
import { useForm, useField } from 'vee-validate'
import * as yup from 'yup'
import { api } from '../../plugins/axios'
import { useNotifyStore } from '../../stores/notify'

interface AdminUserDto {
  id: string
  email: string
  roles: string[]
  createdAtUtc: string
  isLockedOut: boolean
  emailConfirmed: boolean
}

interface RoleDto {
  name: string
}

interface PagedResult<T> {
  page: number
  pageSize: number
  totalCount: number
  items: T[]
}

const notify = useNotifyStore()

const users = ref<AdminUserDto[]>([])
const roles = ref<RoleDto[]>([])
const loading = ref(false)
const search = ref('')
const roleFilter = ref<'all' | string>('all')
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

const createOpen = ref(false)
const roleOpen = ref(false)
const resetOpen = ref(false)
const deleteOpen = ref(false)
const selectedUser = ref<AdminUserDto | null>(null)
const lockingId = ref<string | null>(null)
const deleting = ref(false)

const roleOptions = computed(() => roles.value.map((role) => role.name))
const roleFilterOptions = computed(() => [
  { title: 'All', value: 'all' },
  ...roles.value.map((role) => ({ title: role.name, value: role.name })),
])

const formatUtc = (value: string) => {
  if (!value) return ''
  return new Date(value).toISOString().replace('T', ' ').replace('Z', ' UTC')
}

const newUserSchema = yup.object({
  email: yup.string().required().email().max(256),
  password: yup.string().required().min(6).max(100),
  role: yup.string().required(),
  fullName: yup.string().when('role', {
    is: 'Expert',
    then: (schema) => schema.required().max(200),
    otherwise: (schema) => schema.optional().nullable(),
  }),
  companyName: yup.string().when('role', {
    is: 'Company',
    then: (schema) => schema.required().max(200),
    otherwise: (schema) => schema.optional().nullable(),
  }),
})

const createForm = useForm({
  validationSchema: newUserSchema,
  initialValues: {
    email: '',
    password: '',
    role: '',
    fullName: '',
    companyName: '',
  },
})

const {
  handleSubmit: handleCreateSubmit,
  resetForm: resetCreateForm,
  isSubmitting: creating,
} = createForm

const { value: newEmail, errorMessage: newEmailError } = useField<string>('email', undefined, { form: createForm })
const { value: newPassword, errorMessage: newPasswordError } = useField<string>('password', undefined, {
  form: createForm,
})
const { value: newRole, errorMessage: newRoleError } = useField<string>('role', undefined, { form: createForm })
const { value: newFullName, errorMessage: newFullNameError } = useField<string>('fullName', undefined, {
  form: createForm,
})
const { value: newCompanyName, errorMessage: newCompanyNameError } = useField<string>('companyName', undefined, {
  form: createForm,
})

const roleSchema = yup.object({
  role: yup.string().required(),
  fullName: yup.string().when('role', {
    is: 'Expert',
    then: (schema) => schema.required().max(200),
    otherwise: (schema) => schema.optional().nullable(),
  }),
  companyName: yup.string().when('role', {
    is: 'Company',
    then: (schema) => schema.required().max(200),
    otherwise: (schema) => schema.optional().nullable(),
  }),
})

const roleForm = useForm({
  validationSchema: roleSchema,
  initialValues: {
    role: '',
    fullName: '',
    companyName: '',
  },
})

const {
  handleSubmit: handleRoleSubmit,
  resetForm: resetRoleForm,
  isSubmitting: updatingRole,
} = roleForm

const { value: roleRole, errorMessage: roleRoleError } = useField<string>('role', undefined, { form: roleForm })
const { value: roleFullName, errorMessage: roleFullNameError } = useField<string>('fullName', undefined, {
  form: roleForm,
})
const { value: roleCompanyName, errorMessage: roleCompanyNameError } = useField<string>('companyName', undefined, {
  form: roleForm,
})

const resetSchema = yup.object({
  password: yup.string().required().min(6).max(100),
})

const resetForm = useForm({
  validationSchema: resetSchema,
  initialValues: {
    password: '',
  },
})

const {
  handleSubmit: handleResetSubmit,
  resetForm: resetResetForm,
  isSubmitting: resetting,
} = resetForm

const { value: resetPassword, errorMessage: resetPasswordError } = useField<string>('password', undefined, {
  form: resetForm,
})

const loadRoles = async () => {
  try {
    const response = await api.get<RoleDto[]>('/admin/roles')
    roles.value = response.data
  } catch (error) {
    notify.notify('Failed to load roles.', 'error')
  }
}

const loadUsers = async () => {
  loading.value = true
  try {
    const params: Record<string, string | number> = {
      page: page.value,
      pageSize: pageSize.value,
    }
    if (search.value.trim()) {
      params.search = search.value.trim()
    }
    if (roleFilter.value !== 'all') {
      params.role = roleFilter.value
    }
    const response = await api.get<PagedResult<AdminUserDto>>('/admin/users', { params })
    users.value = response.data.items
    totalCount.value = response.data.totalCount
  } catch (error) {
    notify.notify('Failed to load users.', 'error')
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  page.value = 1
  void loadUsers()
}

const openCreate = () => {
  resetCreateForm()
  createOpen.value = true
}

const openRoleDialog = (user: AdminUserDto) => {
  selectedUser.value = user
  resetRoleForm({
    values: {
      role: user.roles[0] ?? '',
      fullName: '',
      companyName: '',
    },
  })
  roleOpen.value = true
}

const openResetDialog = (user: AdminUserDto) => {
  selectedUser.value = user
  resetResetForm({
    values: {
      password: '',
    },
  })
  resetOpen.value = true
}

const openDeleteDialog = (user: AdminUserDto) => {
  selectedUser.value = user
  deleteOpen.value = true
}

const submitCreate = handleCreateSubmit(async (values) => {
  try {
    await api.post('/admin/users', {
      email: values.email,
      password: values.password,
      role: values.role,
      fullName: values.fullName?.trim() || null,
      companyName: values.companyName?.trim() || null,
    })
    notify.notify('User created.', 'success')
    createOpen.value = false
    await loadUsers()
  } catch (error) {
    notify.notify('Failed to create user.', 'error')
  }
})

const submitRole = handleRoleSubmit(async (values) => {
  if (!selectedUser.value) {
    return
  }
  try {
    await api.put(`/admin/users/${selectedUser.value.id}/role`, {
      role: values.role,
      fullName: values.fullName?.trim() || null,
      companyName: values.companyName?.trim() || null,
    })
    notify.notify('Role updated.', 'success')
    roleOpen.value = false
    await loadUsers()
  } catch (error) {
    notify.notify('Failed to update role.', 'error')
  }
})

const submitReset = handleResetSubmit(async (values) => {
  if (!selectedUser.value) {
    return
  }
  try {
    await api.post(`/admin/users/${selectedUser.value.id}/reset-password`, {
      password: values.password,
    })
    notify.notify('Password reset.', 'success')
    resetOpen.value = false
  } catch (error) {
    notify.notify('Failed to reset password.', 'error')
  }
})

const toggleLock = async (user: AdminUserDto) => {
  lockingId.value = user.id
  try {
    if (user.isLockedOut) {
      await api.post(`/admin/users/${user.id}/unlock`)
      notify.notify('User unlocked.', 'success')
    } else {
      await api.post(`/admin/users/${user.id}/lock`)
      notify.notify('User locked.', 'success')
    }
    await loadUsers()
  } catch (error) {
    notify.notify('Failed to update lock status.', 'error')
  } finally {
    lockingId.value = null
  }
}

const confirmDelete = async () => {
  if (!selectedUser.value) {
    return
  }
  deleting.value = true
  try {
    await api.delete(`/admin/users/${selectedUser.value.id}`)
    notify.notify('User deleted.', 'success')
    deleteOpen.value = false
    await loadUsers()
  } catch (error) {
    notify.notify('Failed to delete user.', 'error')
  } finally {
    deleting.value = false
  }
}

watch(newRole, (value) => {
  if (value !== 'Expert') {
    newFullName.value = ''
  }
  if (value !== 'Company') {
    newCompanyName.value = ''
  }
})

watch(roleRole, (value) => {
  if (value !== 'Expert') {
    roleFullName.value = ''
  }
  if (value !== 'Company') {
    roleCompanyName.value = ''
  }
})

onMounted(async () => {
  await loadRoles()
  await loadUsers()
})
</script>
