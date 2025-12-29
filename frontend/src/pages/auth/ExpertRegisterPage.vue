<template>
  <v-form @submit.prevent="onSubmit">
    <v-text-field
      v-model="fullName"
      label="Full name"
      :error-messages="fullNameError"
      required
    />
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
      autocomplete="new-password"
      required
    />
    <v-btn color="primary" type="submit" block :loading="isSubmitting" class="mb-3">
      Register Expert
    </v-btn>
    <v-btn variant="text" to="/login" block>Back to login</v-btn>
  </v-form>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useForm, useField } from 'vee-validate'
import * as yup from 'yup'
import { api } from '../../plugins/axios'
import { useNotifyStore } from '../../stores/notify'

const notify = useNotifyStore()
const router = useRouter()

const schema = yup.object({
  fullName: yup.string().min(2).required(),
  email: yup.string().email().required(),
  password: yup.string().min(8).required(),
})

const { handleSubmit, isSubmitting } = useForm({
  validationSchema: schema,
})

const { value: fullName, errorMessage: fullNameError } = useField<string>('fullName')
const { value: email, errorMessage: emailError } = useField<string>('email')
const { value: password, errorMessage: passwordError } = useField<string>('password')

const onSubmit = handleSubmit(async (values) => {
  await api.post('/auth/register/expert', values)
  notify.notify('Expert registration submitted. Await admin approval.', 'success')
  await router.push('/login')
})
</script>
