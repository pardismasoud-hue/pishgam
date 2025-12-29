<template>
  <v-card elevation="2">
    <v-card-title>{{ title }}</v-card-title>
    <v-card-text>
      <div class="text-body-2 text-medium-emphasis">
        {{ description }}
      </div>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
const props = withDefaults(
  defineProps<{
    title: string
    description?: string
  }>(),
  {
    description: 'This module will be available in the next phase.',
  }
)

const { title, description } = props
</script>
