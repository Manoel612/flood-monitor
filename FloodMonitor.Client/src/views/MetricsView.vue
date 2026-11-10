<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import L from 'leaflet'
import { Bar } from 'vue-chartjs'
import { BarElement, CategoryScale, Chart as ChartJS, Legend, LinearScale, Tooltip } from 'chart.js'
import { useTheme } from 'vuetify'
import BaseMap from '@/components/BaseMap.vue'
import LocationFilter from '@/components/LocationFilter.vue'
import PageHeader from '@/components/PageHeader.vue'
import PeriodFilter from '@/components/PeriodFilter.vue'
import StatCard from '@/components/StatCard.vue'
import { MAP } from '@/config'
import { getHotspots, getMetricsSummary, getTotalsByMonth, getTotalsByNeighborhood } from '@/services/api'
import { WATER_LEVELS, waterLevelByValue } from '@/utils/waterLevels'
import { formatDate, monthLabel, periodStart } from '@/utils/formatters'
import { locationQuery } from '@/utils/location'

ChartJS.register(BarElement, CategoryScale, Legend, LinearScale, Tooltip)
ChartJS.defaults.font.family = 'Roboto, sans-serif'

const theme = useTheme()

const period = ref('12m')
const location = ref(null)
const summary = ref(null)
const neighborhoods = ref([])
const months = ref([])
const hotspots = ref([])
const loading = ref(false)
const error = ref('')

const locationParams = computed(() => locationQuery(location.value))

async function loadMetrics(fitMap = false) {
  loading.value = true
  error.value = ''
  try {
    const filter = { from: periodStart(period.value), ...locationParams.value }
    const [summaryData, neighborhoodData, hotspotData] = await Promise.all([
      getMetricsSummary(filter),
      getTotalsByNeighborhood(filter),
      getHotspots(filter)
    ])
    summary.value = summaryData
    neighborhoods.value = neighborhoodData
    hotspots.value = hotspotData
    drawHotspots()
    if (fitMap) fitMapToResults()
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

async function loadMonths() {
  try {
    months.value = await getTotalsByMonth(locationParams.value)
  } catch (e) {
    error.value = e.message
  }
}

onMounted(() => {
  loadMetrics()
  loadMonths()
})

watch(period, () => loadMetrics())
watch(location, () => {
  showAllHotspots.value = false
  loadMetrics(true)
  loadMonths()
})

const totalForLevel = (month, levelValue) =>
  month.byWaterLevel.find((item) => item.waterLevel === levelValue)?.total ?? 0

const chartData = computed(() => ({
  labels: months.value.map((m) => monthLabel(m.year, m.month)),
  datasets: WATER_LEVELS.map((level) => ({
    label: level.label,
    data: months.value.map((m) => totalForLevel(m, level.value)),
    backgroundColor: level.color,

    borderColor: theme.current.value.colors.surface,
    borderWidth: 1,
    stack: 'months'
  }))
}))

const chartOptions = computed(() => {
  const { colors, dark } = theme.current.value
  const textColor = colors['on-surface']
  const axis = { ticks: { color: textColor }, grid: { color: dark ? 'rgba(255,255,255,0.12)' : 'rgba(0,0,0,0.08)' } }
  return {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false },
    plugins: {
      legend: {
        position: 'bottom',
        labels: { color: textColor, usePointStyle: true, pointStyle: 'circle', boxWidth: 10, boxHeight: 10 }
      }
    },
    scales: {
      x: { ...axis, stacked: true },
      y: { ...axis, stacked: true, beginAtZero: true, ticks: { ...axis.ticks, precision: 0 } }
    }
  }
})

const chartDescription = computed(() => {
  if (!months.value.length) return 'Gráfico de ocorrências por mês.'
  const peak = months.value.reduce((highest, m) => (m.total > highest.total ? m : highest))
  const total = months.value.reduce((sum, m) => sum + m.total, 0)
  return `Gráfico de barras empilhadas pela altura da água, com ${total} ocorrências nos últimos 12 meses. ` +
    `O mês com mais registros foi ${monthLabel(peak.year, peak.month)}, com ${peak.total}.`
})

const highestNeighborhoodTotal = computed(() => Math.max(1, ...neighborhoods.value.map((n) => n.total)))
const byWaterLevel = computed(() =>
  (summary.value?.byWaterLevel ?? []).map((item) => ({ ...item, level: waterLevelByValue(item.waterLevel) }))
)
const percentage = (part) => (summary.value?.total ? Math.round((part / summary.value.total) * 100) : 0)

const HOTSPOT_LIST_LIMIT = 5
const showAllHotspots = ref(false)
const visibleHotspots = computed(() =>
  showAllHotspots.value ? hotspots.value : hotspots.value.slice(0, HOTSPOT_LIST_LIMIT)
)

let hotspotMap = null
let hotspotLayer = null
let addressArea = null

function onMapReady(map) {
  hotspotMap = map
  hotspotLayer = L.layerGroup().addTo(map)
  drawHotspots()
  fitMapToResults()
}

function drawHotspots() {
  if (!hotspotLayer) return
  hotspotLayer.clearLayers()
  addressArea = null
  const { colors } = theme.current.value

  if (location.value?.type === 'address') {
    addressArea = L.circle([location.value.lat, location.value.lon], {
      radius: location.value.radius,
      color: colors.primary,
      weight: 2,
      dashArray: '6 6',
      fillColor: colors.primary,
      fillOpacity: 0.06,
      interactive: false
    }).addTo(hotspotLayer)
  }

  const color = colors.error
  for (const hotspot of hotspots.value) {
    L.circle([hotspot.latitude, hotspot.longitude], {
      radius: 80 + hotspot.occurrences * 25,
      color,
      weight: 2,
      fillColor: color,
      fillOpacity: 0.3
    })
      .bindTooltip(createTooltip(hotspot))
      .addTo(hotspotLayer)
  }
}

function createTooltip(hotspot) {
  const content = document.createElement('span')
  content.textContent = `${hotspot.neighborhood ?? 'Local'}: ${hotspot.occurrences} ocorrências em ${hotspot.distinctDays} dias`
  return content
}

function fitMapToResults() {
  if (!hotspotMap) return
  if (addressArea) {
    hotspotMap.fitBounds(addressArea.getBounds(), { padding: [16, 16] })
  } else if (location.value && hotspots.value.length) {
    const bounds = L.latLngBounds(hotspots.value.map((h) => [h.latitude, h.longitude]))
    hotspotMap.fitBounds(bounds, { padding: [48, 48], maxZoom: 15 })
  } else {
    hotspotMap.setView(MAP.center, MAP.zoom)
  }
}

watch(() => theme.current.value.dark, drawHotspots)
</script>

<template>
  <div class="page-content">
    <PageHeader title="Métricas das enchentes" subtitle="Frequência e localização das ocorrências registradas.">
      <template #actions>
        <PeriodFilter v-model="period" />
      </template>
    </PageHeader>

    <LocationFilter v-model="location" class="section" />

    <v-alert v-if="error" type="error" color="error-container" class="section">{{ error }}</v-alert>
    <v-progress-linear v-if="loading" indeterminate class="section" />

    <v-row class="layout-grid section">
      <v-col cols="2" md="3">
        <StatCard label="No período" :value="summary?.total" icon="mdi-map-marker-multiple-outline" />
      </v-col>
      <v-col cols="2" md="3">
        <StatCard label="Últimos 7 dias" :value="summary?.last7Days" icon="mdi-calendar-week" />
      </v-col>
      <v-col cols="2" md="3">
        <StatCard label="Últimos 30 dias" :value="summary?.last30Days" icon="mdi-calendar-month" />
      </v-col>
      <v-col cols="2" md="3">
        <StatCard label="Bairro mais afetado" :value="summary?.mostAffectedNeighborhood ?? '–'" icon="mdi-home-alert-outline" />
      </v-col>

      <v-col cols="4" md="7">
        <v-card class="h-100">
          <v-card-item>
            <v-card-title class="text-title-large">Ocorrências por mês</v-card-title>
            <v-card-subtitle>Últimos 12 meses</v-card-subtitle>
          </v-card-item>
          <v-card-text>
            <div class="chart" role="img" :aria-label="chartDescription">
              <Bar :data="chartData" :options="chartOptions" />
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="4" md="5">
        <v-card class="h-100">
          <v-card-item>
            <v-card-title class="text-title-large">Altura da água</v-card-title>
            <v-card-subtitle>Como as ocorrências do período se dividem</v-card-subtitle>
          </v-card-item>
          <v-card-text>
            <div v-for="item in byWaterLevel" :key="item.level.value" class="mb-4">
              <div class="d-flex justify-space-between text-body-medium mb-1">
                <span>{{ item.level.label }}</span>
                <span>{{ item.total }} ({{ percentage(item.total) }}%)</span>
              </div>
              <v-progress-linear
                :model-value="percentage(item.total)"
                :color="item.level.color"
                height="10"
                :aria-label="`${item.level.label}: ${percentage(item.total)}%`"
              />
            </div>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="4" md="12">
        <v-card>
          <v-card-item>
            <v-card-title class="text-title-large">Pontos críticos</v-card-title>
            <v-card-subtitle>Locais que alagaram em pelo menos dois dias diferentes</v-card-subtitle>
          </v-card-item>
          <v-card-text>
            <v-row class="layout-grid">
              <v-col cols="4" md="12" lg="8">
                <v-sheet rounded="lg" class="overflow-hidden hotspot-map" border>
                  <BaseMap height="100%" description="Mapa dos pontos críticos de alagamento" @ready="onMapReady" />
                </v-sheet>
              </v-col>
              <v-col cols="4" md="12" lg="4">
                <p v-if="!hotspots.length" class="text-body-medium">Nenhum ponto recorrente no período.</p>
                <template v-else>
                  <v-list class="pa-0">
                    <v-list-item
                      v-for="hotspot in visibleHotspots"
                      :key="`${hotspot.latitude},${hotspot.longitude}`"
                      :title="hotspot.neighborhood ?? 'Bairro não identificado'"
                      :subtitle="`${hotspot.occurrences} ocorrências em ${hotspot.distinctDays} dias diferentes`"
                    >
                      <template #prepend>
                        <v-avatar color="error-container" size="32" class="mr-3">
                          <v-icon icon="mdi-alert-outline" size="18" />
                        </v-avatar>
                      </template>
                    </v-list-item>
                  </v-list>
                  <v-btn
                    v-if="hotspots.length > HOTSPOT_LIST_LIMIT"
                    variant="text"
                    color="primary"
                    class="mt-2"
                    :prepend-icon="showAllHotspots ? 'mdi-chevron-up' : 'mdi-chevron-down'"
                    :aria-expanded="showAllHotspots"
                    @click="showAllHotspots = !showAllHotspots"
                  >
                    {{ showAllHotspots ? 'Mostrar menos' : `Ver todos (${hotspots.length})` }}
                  </v-btn>
                </template>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>
      </v-col>

      <v-col cols="4" md="12">
        <v-card>
          <v-card-item>
            <v-card-title class="text-title-large">Bairros com mais registros</v-card-title>
          </v-card-item>
          <v-card-text>
            <p v-if="!neighborhoods.length" class="text-body-medium">Nenhuma ocorrência com bairro no período.</p>
            <v-table v-else>
              <thead>
                <tr>
                  <th scope="col">Bairro</th>
                  <th scope="col" class="text-end">Ocorrências</th>
                  <th scope="col" class="text-end">Última</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in neighborhoods" :key="row.neighborhood">
                  <td>
                    {{ row.neighborhood }}
                    <v-progress-linear :model-value="(row.total / highestNeighborhoodTotal) * 100" height="4" aria-hidden="true" />
                  </td>
                  <td class="text-end">{{ row.total }}</td>
                  <td class="text-end">{{ formatDate(row.lastOccurrence) }}</td>
                </tr>
              </tbody>
            </v-table>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<style scoped>
.chart {
  position: relative;
  height: 320px;
}

.hotspot-map {
  height: 300px;
}

@media (min-width: 1145px) {
  .hotspot-map {
    height: 440px;
  }
}
</style>
