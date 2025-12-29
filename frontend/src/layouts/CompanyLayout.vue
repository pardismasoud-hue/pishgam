<template>
  <v-layout class="fill-height">
    <v-navigation-drawer permanent>
      <v-list density="compact" nav>
        <v-list-item title="MSP Company" subtitle="Customer Portal" />
        <v-divider class="my-2" />
        <v-list-item v-for="link in links" :key="link.to" :to="link.to" :title="link.title" />
      </v-list>
    </v-navigation-drawer>
    <v-main>
      <v-app-bar flat color="transparent">
        <v-spacer />
        <v-chip
          v-if="auth.isAdmin && auth.isActingAsCompany && auth.actingCompanyLabel"
          class="mr-2"
          color="secondary"
          variant="tonal"
        >
          Acting as Company: {{ auth.actingCompanyLabel }}
        </v-chip>
        <v-chip v-if="auth.email" class="mr-2" color="primary" variant="tonal">
          {{ auth.email }}
        </v-chip>
        <v-btn
          v-if="auth.isAdmin && auth.isActingAsCompany"
          variant="text"
          @click="exitActing"
        >
          Back to Admin
        </v-btn>
        <v-btn variant="text" @click="logout">Logout</v-btn>
      </v-app-bar>
      <v-container class="py-6">
        <router-view />
      </v-container>
    </v-main>
  </v-layout>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

const links = [
  { title: 'Dashboard', to: '/company' },
  { title: 'Assets', to: '/company/assets' },
  { title: 'Contracts', to: '/company/contracts' },
  { title: 'Tickets', to: '/company/tickets' },
  { title: 'Satisfaction', to: '/company/satisfaction' },
]

const logout = () => {
  auth.logout()
  router.push('/login')
}

const exitActing = () => {
  auth.clearActingContext()
  router.push('/admin')
}
</script>
