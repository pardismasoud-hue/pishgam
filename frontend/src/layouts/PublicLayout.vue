<template>
  <v-main class="public-shell">
    <v-container class="fill-height d-flex align-center justify-center">
      <v-sheet class="pa-6 rounded-lg auth-card" elevation="4">
        <div class="text-h5 mb-2">Managed Services Platform</div>
        <div class="text-subtitle-2 mb-6 text-medium-emphasis">
          Secure access for Admins, Experts, and Companies
        </div>
        <router-view />
      </v-sheet>
    </v-container>
  </v-main>
</template>

<style scoped>
.public-shell {
  min-height: 100vh;
  background: linear-gradient(135deg, #f6f2ea 0%, #e9f1f4 100%);
}

.auth-card {
  width: 100%;
  max-width: 460px;
}
</style>
