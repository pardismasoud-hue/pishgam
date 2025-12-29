<template>
  <v-form @submit.prevent="onSubmit">
    <v-text-field
      v-model="email"
      label="Email"
      type="email"
      :error-messages="emailError"
      autocomplete="username"
      required
    />
    <v-text-field
      v-model="password"
      label="Password"
      type="password"
      :error-messages="passwordError"
      autocomplete="current-password"
      required
    />
    <v-btn
      color="primary"
      type="submit"
      block
      :loading="isSubmitting"
      class="mb-3"
    >
      Sign In
    </v-btn>
    <div class="d-flex justify-space-between">
      <v-btn variant="text" :to="'/register/company'">Register Company</v-btn>
      <v-btn variant="text" :to="'/register/expert'">Register Expert</v-btn>
    </div>
  </v-form>
</template>

<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import { useForm, useField } from 'vee-validate'
import * as yup from 'yup'
import { useAuthStore } from '../../stores/auth'
import { useNotifyStore } from '../../stores/notify'

const auth = useAuthStore()
const notify = useNotifyStore()
const router = useRouter()
const route = useRoute()

const schema = yup.object({
  email: yup.string().email().required(),
  password: yup.string().required(),
})

const { handleSubmit, isSubmitting } = useForm({
  validationSchema: schema,
})

const { value: email, errorMessage: emailError } = useField<string>('email')
const { value: password, errorMessage: passwordError } = useField<string>('password')

const onSubmit = handleSubmit(async (values) => {
  try {
    const role = await auth.login(values.email, values.password)
    const redirect = (route.query.redirect as string | undefined) ?? `/${role.toLowerCase()}`
    await router.push(redirect)
  } catch (error) {
    notify.notify('Login failed. Check your credentials.', 'error')
  }
})
</script>
