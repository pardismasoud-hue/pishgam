<template>
  <v-snackbar v-model="notify.show" :color="color" :timeout="notify.timeout">
    {{ notify.message }}
  </v-snackbar>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useNotifyStore } from '../stores/notify'

const notify = useNotifyStore()

const color = computed(() => {
  switch (notify.level) {
    case 'success':
      return 'success'
    case 'warning':
      return 'warning'
    case 'error':
      return 'error'
    default:
      return 'info'
  }
})
</script>
