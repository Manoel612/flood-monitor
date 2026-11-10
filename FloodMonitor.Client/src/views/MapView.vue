<script setup>
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import L from 'leaflet'
import BaseMap from '@/components/BaseMap.vue'
import PeriodFilter from '@/components/PeriodFilter.vue'
import WaterLevelLegend from '@/components/WaterLevelLegend.vue'
import { useMarkerBorderColor } from '@/composables/useMarkerBorderColor'
import { listOccurrences } from '@/services/api'
import { waterLevelByValue } from '@/utils/waterLevels'
import { formatDateTime, periodStart } from '@/utils/formatters'

const router = useRouter()
const period = ref('12m')
const total = ref(null)
const loading = ref(false)
const error = ref('')

const legendTitle = computed(() =>
  total.value === null ? 'Altura da água' : `${total.value} ocorrência${total.value === 1 ? '' : 's'} no período`
)

const { mdAndUp } = useDisplay()

const markerBorderColor = useMarkerBorderColor()

let map = null
let markerLayer = null

function onMapReady(instance) {
  map = instance
  map.zoomControl.setPosition('bottomright')
  markerLayer = L.layerGroup().addTo(map)
  loadOccurrences()
}

async function loadOccurrences() {
  if (!markerLayer) return
  loading.value = true
  try {
    const occurrences = await listOccurrences({ from: periodStart(period.value) })
    markerLayer.clearLayers()

    for (const occurrence of [...occurrences].reverse()) {
      const level = waterLevelByValue(occurrence.waterLevel)
      L.circleMarker([occurrence.latitude, occurrence.longitude], {
        radius: level.markerRadius,
        color: markerBorderColor.value,
        weight: 2,
        fillColor: level.color,
        fillOpacity: 0.85
      })
        .bindPopup(() => createPopup(occurrence, level))
        .addTo(markerLayer)
    }
    total.value = occurrences.length
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

function createPopup(occurrence, level) {
  const content = document.createElement('div')

  const title = document.createElement('strong')
  title.textContent = level.label

  const details = document.createElement('div')
  details.textContent = [formatDateTime(occurrence.occurredAt), occurrence.neighborhood].filter(Boolean).join(' · ')

  const detailsRoute = { name: 'occurrence', params: { id: occurrence.id } }
  const link = document.createElement('a')
  link.href = router.resolve(detailsRoute).href
  link.textContent = 'Ver detalhes'
  link.addEventListener('click', (event) => {
    event.preventDefault()
    router.push(detailsRoute)
  })

  content.append(title, details, link)
  return content
}

function goToMyLocation() {
  if (!('geolocation' in navigator) || !window.isSecureContext) {
    error.value = 'O navegador não liberou sua localização nesta página.'
    return
  }
  navigator.geolocation.getCurrentPosition(
    (position) => map?.setView([position.coords.latitude, position.coords.longitude], 15),
    () => (error.value = 'Não foi possível obter sua localização.'),
    { timeout: 15000, maximumAge: 60000 }
  )
}

watch(period, loadOccurrences)
watch(markerBorderColor, (color) => markerLayer?.eachLayer((marker) => marker.setStyle({ color })))
</script>

<template>
  <div class="map-page">
    <BaseMap description="Mapa das ocorrências de enchente" @ready="onMapReady" />

    <v-progress-linear v-if="loading" indeterminate color="primary" class="loading-bar" />

    <div class="overlay-panel">
      <v-sheet rounded="md" elevation="3" class="period-sheet">
        <PeriodFilter v-model="period" />
      </v-sheet>
      <WaterLevelLegend :title="legendTitle" collapsible :initially-open="mdAndUp" class="mt-2 legend" />
    </div>

    <v-btn
      class="locate-button"
      icon="mdi-crosshairs-gps"
      elevation="3"
      rounded="md"
      aria-label="Mostrar minha região no mapa"
      @click="goToMyLocation"
    />

    <primary-button
      class="report-button"
      size="x-large"
      elevation="8"
      prepend-icon="mdi-plus"
      :to="{ name: 'report' }"
    >
      Registrar enchente
    </primary-button>

    <v-snackbar :model-value="!!error" color="error" @update:model-value="error = ''">{{ error }}</v-snackbar>
  </div>
</template>

<style scoped>
.map-page {
  position: relative;
  height: calc(100dvh - var(--v-layout-top, 56px) - var(--v-layout-bottom, 0px));
}

.period-sheet {
  width: fit-content;
  max-width: 100%;
  overflow-x: auto;
}

.loading-bar {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  z-index: 1001;
}

.overlay-panel {
  position: absolute;
  top: 16px;
  left: 16px;
  z-index: 1001;
  max-width: calc(100% - 32px);
}

.legend {
  width: fit-content;
}

.locate-button {
  position: absolute;
  right: 10px;
  bottom: 104px;
  z-index: 1001;
}

.report-button {
  position: absolute;
  bottom: 32px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 1001;
}
</style>
