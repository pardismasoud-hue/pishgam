<template>
  <v-form @submit.prevent="onSubmit">
    <v-text-field
      v-model="companyName"
      label="Company name"
      :error-messages="companyNameError"
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
      Register Company
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
  companyName: yup.string().min(2).required(),
  email: yup.string().email().required(),
  password: yup.string().min(8).required(),
})

const { handleSubmit, isSubmitting } = useForm({
  validationSchema: schema,
})

const { value: companyName, errorMessage: companyNameError } = useField<string>('companyName')
const { value: email, errorMessage: emailError } = useField<string>('email')
const { value: password, errorMessage: passwordError } = useField<string>('password')

const onSubmit = handleSubmit(async (values) => {
  await api.post('/auth/register/company', values)
  notify.notify('Company account created. You can sign in now.', 'success')
  await router.push('/login')
})
</script>
