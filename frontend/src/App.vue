<template>
  <v-app>
    <router-view />
    <NotifySnackbar />
  </v-app>
</template>

<script setup lang="ts">
import NotifySnackbar from './components/NotifySnackbar.vue'
</script>
