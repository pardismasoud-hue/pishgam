<template>
  <div>
    <div class="d-flex flex-wrap align-center justify-space-between mb-4">
      <div>
        <h2 class="text-h5 font-weight-bold">KPI</h2>
        <div class="text-body-2 text-medium-emphasis">Review your operational performance.</div>
      </div>
      <v-btn color="primary" variant="tonal" :loading="loading" @click="loadKpi">Refresh</v-btn>
    </div>

    <v-alert v-if="errorMessage" type="error" variant="tonal" class="mb-4">
      {{ errorMessage }}
    </v-alert>

    <v-row dense>
      <v-col cols="12" md="4">
        <v-card rounded="lg" elevation="2">
          <v-card-text>
            <div class="text-body-2 text-medium-emphasis">Assigned tickets</div>
            <div class="text-h4 font-weight-bold">{{ kpi?.totalAssignedTickets ?? '-' }}</div>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" md="4">
        <v-card rounded="lg" elevation="2">
          <v-card-text>
            <div class="text-body-2 text-medium-emphasis">Resolved tickets</div>
            <div class="text-h4 font-weight-bold">{{ kpi?.totalResolvedTickets ?? '-' }}</div>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" md="4">
        <v-card rounded="lg" elevation="2">
          <v-card-text>
            <div class="text-body-2 text-medium-emphasis">Logged minutes</div>
            <div class="text-h4 font-weight-bold">{{ kpi?.totalLoggedMinutes ?? '-' }}</div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-row dense class="mt-2">
      <v-col cols="12" md="6">
        <v-card rounded="lg" elevation="2">
          <v-card-text>
            <div class="text-subtitle-1 font-weight-medium mb-2">Response and resolution</div>
            <v-row dense>
              <v-col cols="12" sm="6">
                <div class="text-body-2 text-medium-emphasis">Avg first response (min)</div>
                <div class="text-h6">{{ formatNumber(kpi?.averageFirstResponseMinutes) }}</div>
              </v-col>
              <v-col cols="12" sm="6">
                <div class="text-body-2 text-medium-emphasis">Avg resolution (min)</div>
                <div class="text-h6">{{ formatNumber(kpi?.averageResolutionMinutes) }}</div>
              </v-col>
              <v-col cols="12" sm="6">
                <div class="text-body-2 text-medium-emphasis">First response breaches</div>
                <div class="text-h6">{{ kpi?.firstResponseBreachCount ?? '-' }}</div>
              </v-col>
              <v-col cols="12" sm="6">
                <div class="text-body-2 text-medium-emphasis">Resolution breaches</div>
                <div class="text-h6">{{ kpi?.resolutionBreachCount ?? '-' }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" md="6">
        <v-card rounded="lg" elevation="2">
          <v-card-text>
            <div class="text-subtitle-1 font-weight-medium mb-2">Customer satisfaction</div>
            <v-row dense>
              <v-col cols="12" sm="6">
                <div class="text-body-2 text-medium-emphasis">Responses captured</div>
                <div class="text-h6">{{ kpi?.satisfactionCount ?? '-' }}</div>
              </v-col>
              <v-col cols="12" sm="6">
                <div class="text-body-2 text-medium-emphasis">Average rating</div>
                <div class="text-h6">{{ formatNumber(kpi?.averageRating) }}</div>
              </v-col>
              <v-col cols="12" sm="6">
                <div class="text-body-2 text-medium-emphasis">Response time rating</div>
                <div class="text-h6">{{ formatNumber(kpi?.averageResponseTimeRating) }}</div>
              </v-col>
              <v-col cols="12" sm="6">
                <div class="text-body-2 text-medium-emphasis">Resolution quality rating</div>
                <div class="text-h6">{{ formatNumber(kpi?.averageResolutionQualityRating) }}</div>
              </v-col>
              <v-col cols="12" sm="6">
                <div class="text-body-2 text-medium-emphasis">Communication rating</div>
                <div class="text-h6">{{ formatNumber(kpi?.averageCommunicationRating) }}</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api } from '../../plugins/axios'
import { useNotifyStore } from '../../stores/notify'

interface ExpertKpiDto {
  expertProfileId: string
  totalAssignedTickets: number
  totalResolvedTickets: number
  averageFirstResponseMinutes?: number | null
  averageResolutionMinutes?: number | null
  firstResponseBreachCount: number
  resolutionBreachCount: number
  satisfactionCount: number
  averageRating?: number | null
  averageResponseTimeRating?: number | null
  averageResolutionQualityRating?: number | null
  averageCommunicationRating?: number | null
  totalLoggedMinutes: number
}

const notify = useNotifyStore()
const loading = ref(false)
const kpi = ref<ExpertKpiDto | null>(null)
const errorMessage = ref('')

const formatNumber = (value?: number | null) => {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return '-'
  }
  return value.toFixed(1)
}

const loadKpi = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const response = await api.get<ExpertKpiDto>('/expert/kpi')
    kpi.value = response.data
  } catch (error) {
    errorMessage.value = 'Failed to load KPI data.'
    notify.notify('Failed to load KPI data.', 'error')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  void loadKpi()
})
</script>
