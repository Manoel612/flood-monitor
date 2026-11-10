<script setup>
import { onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import L from 'leaflet'
import BaseMap from '@/components/BaseMap.vue'
import DeleteOccurrenceDialog from '@/components/DeleteOccurrenceDialog.vue'
import WaterLevelChip from '@/components/WaterLevelChip.vue'
import { useMarkerBorderColor } from '@/composables/useMarkerBorderColor'
import { isAdmin } from '@/services/adminSession'
import { getOccurrence } from '@/services/api'
import { waterLevelByValue } from '@/utils/waterLevels'
import { formatDateTime } from '@/utils/formatters'
import { cityStateAndDate } from '@/utils/location'

const props = defineProps({
  id: { type: String, required: true }
})

const occurrence = ref(null)
const level = ref(null)
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    occurrence.value = await getOccurrence(props.id)
    level.value = waterLevelByValue(occurrence.value.waterLevel)
  } catch (e) {
    error.value = e.status === 404 ? 'Não encontramos essa ocorrência.' : e.message
  } finally {
    loading.value = false
  }
})

const markerBorderColor = useMarkerBorderColor()
let marker = null

function onMapReady(map) {
  const { latitude, longitude } = occurrence.value
  marker = L.circleMarker([latitude, longitude], {
    radius: 12,
    color: markerBorderColor.value,
    weight: 3,
    fillColor: level.value.color,
    fillOpacity: 0.9
  }).addTo(map)
}

watch(markerBorderColor, (color) => marker?.setStyle({ color }))

const router = useRouter()
function goBack() {
  if (window.history.state?.back) router.back()
  else router.push({ name: 'map' })
}

const deleteDialogOpen = ref(false)

function onDeleted() {
  router.replace({ name: 'occurrences' })
}
</script>

<template>
  <div class="page-content">
    <v-row class="layout-grid justify-center">
      <v-col cols="4" md="8" lg="6">
        <v-btn variant="text" color="primary" prepend-icon="mdi-arrow-left" class="section" @click="goBack">
          Voltar
        </v-btn>

        <div v-if="loading" class="d-flex justify-center py-8">
          <v-progress-circular indeterminate aria-label="Carregando ocorrência" />
        </div>

        <v-alert v-else-if="error" type="warning" class="section">{{ error }}</v-alert>

        <v-card v-else class="section">
          <v-img
            v-if="occurrence.photoUrl"
            :src="occurrence.photoUrl"
            max-height="360"
            cover
            alt="Foto enviada com o registro"
          />
          <v-card-item>
            <WaterLevelChip :water-level="occurrence.waterLevel" class="mb-3" />
            <h1 class="text-headline-small">{{ occurrence.neighborhood ?? 'Bairro não identificado' }}</h1>
            <p class="text-body-large text-medium-emphasis mt-1">
              {{ cityStateAndDate(occurrence) }}
            </p>
          </v-card-item>
          <v-card-text v-if="occurrence.description" class="text-body-large">{{ occurrence.description }}</v-card-text>
          <v-sheet class="mx-4 mb-4 overflow-hidden" rounded="lg" border>
            <BaseMap
              :center="[occurrence.latitude, occurrence.longitude]"
              :zoom="16"
              height="260px"
              description="Local da ocorrência"
              @ready="onMapReady"
            />
          </v-sheet>
          <v-divider />
          <v-card-text class="d-flex align-center ga-2 text-body-small text-medium-emphasis">
            <v-icon icon="mdi-account-off-outline" size="18" />
            Registrado em {{ formatDateTime(occurrence.reportedAt) }} por um morador, sem identificação.
          </v-card-text>
          <template v-if="isAdmin">
            <v-divider />
            <v-card-actions class="px-4 py-3">
              <v-btn variant="text" color="error" prepend-icon="mdi-delete-outline" @click="deleteDialogOpen = true">
                Excluir ocorrência
              </v-btn>
            </v-card-actions>
          </template>
        </v-card>

        <DeleteOccurrenceDialog v-if="occurrence" v-model="deleteDialogOpen" :occurrence="occurrence" @deleted="onDeleted" />
      </v-col>
    </v-row>
  </div>
</template>
