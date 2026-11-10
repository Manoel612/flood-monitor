<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import L from 'leaflet'
import { Bar } from 'vue-chartjs'
import {
  BarElement,
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LinearScale,
  LineController,
  LineElement,
  PointElement,
  Tooltip
} from 'chart.js'
import { useTheme } from 'vuetify'
import BaseMap from '@/components/BaseMap.vue'
import PageHeader from '@/components/PageHeader.vue'
import StatCard from '@/components/StatCard.vue'
import { useMarkerBorderColor } from '@/composables/useMarkerBorderColor'
import { getCityRainfall, getRainfallCities } from '@/services/api'
import { monthLabel } from '@/utils/formatters'

ChartJS.register(BarElement, CategoryScale, Legend, LinearScale, LineController, LineElement, PointElement, Tooltip)

const SYNC_RETRY_MS = 15000
const MAX_SYNC_RETRIES = 8

const theme = useTheme()
const markerBorderColor = useMarkerBorderColor()

const cities = ref([])
const selectedCity = ref(null)
const rainfall = ref(null)
const loading = ref(false)
const error = ref('')

const cityOptions = computed(() =>
  cities.value.map((c) => ({
    title: `${c.city}, ${c.state} (${c.occurrences} ocorrência${c.occurrences === 1 ? '' : 's'})`,
    value: `${c.city}|${c.state}`
  }))
)

onMounted(async () => {
  try {
    cities.value = await getRainfallCities()
    if (cities.value.length) selectedCity.value = cityOptions.value[0].value
  } catch (e) {
    error.value = e.message
  }
})

let retryTimer = null
let retries = 0

async function loadRainfall() {
  clearTimeout(retryTimer)
  if (!selectedCity.value) return
  const [city, state] = selectedCity.value.split('|')
  loading.value = true
  error.value = ''
  try {
    const data = await getCityRainfall(city, state)
    if (selectedCity.value !== `${city}|${state}`) return
    rainfall.value = data
    drawMap()
    if (data.syncPending && retries < MAX_SYNC_RETRIES) {
      retries++
      retryTimer = setTimeout(loadRainfall, SYNC_RETRY_MS)
    }
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

watch(selectedCity, () => {
  retries = 0
  rainfall.value = null
  loadRainfall()
})

onBeforeUnmount(() => clearTimeout(retryTimer))

const months = computed(() => rainfall.value?.occurrences ?? [])
const mainSeries = computed(() => rainfall.value?.measured ?? rainfall.value?.estimated ?? null)
const mainSeriesIsMeasured = computed(() => !!rainfall.value?.measured)

const totalRain = computed(() => {
  const values = mainSeries.value?.months.map((m) => m.millimeters).filter((v) => v != null) ?? []
  return values.length ? `${Math.round(values.reduce((sum, v) => sum + v, 0))} mm` : '–'
})

const rainiestMonth = computed(() => {
  const withData = mainSeries.value?.months.filter((m) => m.millimeters != null) ?? []
  if (!withData.length) return '–'
  const top = withData.reduce((highest, m) => (m.millimeters > highest.millimeters ? m : highest))
  return `${monthLabel(top.year, top.month)} · ${Math.round(top.millimeters)} mm`
})

const totalOccurrences = computed(() => months.value.reduce((sum, m) => sum + m.total, 0))

const measuredLabel = computed(() => {
  const station = rainfall.value?.measured?.station
  return station ? `Medida: CEMADEN (${station.name})` : 'Medida: CEMADEN'
})

const chartData = computed(() => {
  const { colors } = theme.current.value
  const datasets = []
  if (rainfall.value?.measured) {
    datasets.push({
      label: measuredLabel.value,
      data: rainfall.value.measured.months.map((m) => m.millimeters),
      backgroundColor: colors.primary,
      yAxisID: 'rain',
      order: 2
    })
  }
  if (rainfall.value?.estimated) {
    datasets.push({
      label: 'Estimada: Open-Meteo',
      data: rainfall.value.estimated.months.map((m) => m.millimeters),
      backgroundColor: colors.secondary,
      yAxisID: 'rain',
      order: 3
    })
  }
  datasets.push({
    type: 'line',
    label: 'Enchentes registradas',
    data: months.value.map((m) => m.total),
    borderColor: colors.error,
    backgroundColor: colors.error,
    pointRadius: 4,
    cubicInterpolationMode: 'monotone',
    yAxisID: 'occurrences',
    order: 1
  })
  return { labels: months.value.map((m) => monthLabel(m.year, m.month)), datasets }
})

const chartOptions = computed(() => {
  const { colors, dark } = theme.current.value
  const textColor = colors['on-surface']
  const grid = { color: dark ? 'rgba(255,255,255,0.12)' : 'rgba(0,0,0,0.08)' }
  return {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false },
    plugins: {
      legend: {
        position: 'bottom',
        labels: { color: textColor, usePointStyle: true, pointStyle: 'circle', boxWidth: 10, boxHeight: 10 }
      },
      tooltip: {
        callbacks: {
          label: (item) => {
            if (item.dataset.yAxisID === 'occurrences') return `${item.dataset.label}: ${item.raw}`
            return item.raw == null ? `${item.dataset.label}: sem dados` : `${item.dataset.label}: ${item.raw} mm`
          }
        }
      }
    },
    scales: {
      x: { ticks: { color: textColor }, grid },
      rain: {
        position: 'left',
        beginAtZero: true,
        title: { display: true, text: 'Chuva (mm)', color: textColor },
        ticks: { color: textColor },
        grid
      },
      occurrences: {
        position: 'right',
        beginAtZero: true,
        title: { display: true, text: 'Enchentes', color: textColor },
        ticks: { color: textColor, precision: 0 },
        grid: { drawOnChartArea: false }
      }
    }
  }
})

const chartDescription = computed(() =>
  `Gráfico de barras com a chuva por mês em milímetros e linha com as enchentes registradas por mês. ` +
  `Chuva nos 12 meses: ${totalRain.value}. Mês mais chuvoso: ${rainiestMonth.value}. ` +
  `Enchentes registradas: ${totalOccurrences.value}.`
)

let map = null
let layer = null

const pinIcon = L.divIcon({
  className: 'map-pin',
  html: '<span class="mdi mdi-map-marker"></span>',
  iconSize: [48, 48],
  iconAnchor: [24, 46]
})

function onMapReady(instance) {
  map = instance
  layer = L.layerGroup().addTo(map)
  drawMap()
}

function textTooltip(text) {
  const content = document.createElement('span')
  content.textContent = text
  return content
}

function drawMap() {
  if (!layer || !rainfall.value) return
  layer.clearLayers()
  const { latitude, longitude, city, stations, measured } = rainfall.value
  const points = [[latitude, longitude]]

  L.marker([latitude, longitude], { icon: pinIcon, title: `Centro das ocorrências de ${city}` })
    .bindTooltip(textTooltip(`Centro das ocorrências registradas em ${city}`))
    .addTo(layer)

  for (const station of stations) {
    const isUsed = station.code === measured?.station?.code
    L.circleMarker([station.latitude, station.longitude], {
      radius: isUsed ? 9 : 6,
      color: markerBorderColor.value,
      weight: 2,
      fillColor: theme.current.value.colors.primary,
      fillOpacity: isUsed ? 0.95 : 0.5
    })
      .bindTooltip(textTooltip(`${station.name} · ${station.distanceKm} km${isUsed ? ' · usada no gráfico' : ''}`))
      .addTo(layer)
    points.push([station.latitude, station.longitude])
  }

  if (points.length > 1) map.fitBounds(L.latLngBounds(points), { padding: [32, 32], maxZoom: 13 })
  else map.setView(points[0], 11)
}

watch(() => theme.current.value.dark, drawMap)
</script>

<template>
  <div class="page-content">
    <PageHeader title="Chuvas" subtitle="Chuva por mês comparada às enchentes registradas pelos moradores." />

    <v-alert v-if="error" type="error" color="error-container" class="section" role="alert">{{ error }}</v-alert>

    <v-empty-state
      v-if="!cities.length && !error"
      icon="mdi-weather-cloudy"
      title="Nenhuma cidade com ocorrências ainda"
      text="A seção de chuvas aparece para as cidades que já têm enchentes registradas no app."
      class="section"
    >
      <template #actions>
        <primary-button prepend-icon="mdi-plus" :to="{ name: 'report' }">Registrar enchente</primary-button>
      </template>
    </v-empty-state>

    <template v-else-if="cities.length">
      <v-card class="section">
        <v-card-text>
          <v-select
            v-model="selectedCity"
            :items="cityOptions"
            label="Cidade"
            prepend-inner-icon="mdi-city-variant-outline"
            hide-details
          />
        </v-card-text>
      </v-card>

      <v-progress-linear v-if="loading && !rainfall" indeterminate class="section" />

      <v-alert v-if="rainfall?.syncPending" type="info" color="surface-variant" class="section">
        Buscando os dados de chuva desta cidade. Esta página se atualiza sozinha em alguns instantes.
      </v-alert>

      <v-alert v-if="rainfall && !rainfall.measurementsEnabled" type="info" color="surface-variant" class="section">
        As medições dos pluviômetros do CEMADEN ainda não estão ativadas neste servidor. Por enquanto, o gráfico mostra
        só a estimativa do Open-Meteo.
      </v-alert>
      <v-alert
        v-else-if="rainfall && !rainfall.measured && !rainfall.syncPending"
        type="info"
        color="surface-variant"
        class="section"
      >
        Ainda não há medições do CEMADEN para esta cidade. Elas podem estar sendo carregadas, ou a cidade pode não
        ter pluviômetros da rede. Enquanto isso, o gráfico usa a estimativa do Open-Meteo.
      </v-alert>

      <v-row v-if="rainfall" class="layout-grid section">
        <v-col cols="4" md="4">
          <StatCard
            :label="mainSeriesIsMeasured ? 'Chuva medida em 12 meses' : 'Chuva estimada em 12 meses'"
            :value="totalRain"
            icon="mdi-weather-pouring"
          />
        </v-col>
        <v-col cols="2" md="4">
          <StatCard label="Mês mais chuvoso" :value="rainiestMonth" icon="mdi-calendar-month" />
        </v-col>
        <v-col cols="2" md="4">
          <StatCard label="Enchentes registradas" :value="totalOccurrences" icon="mdi-home-flood" />
        </v-col>

        <v-col cols="4" md="12">
          <v-card>
            <v-card-item>
              <v-card-title class="text-title-large">Chuva e enchentes por mês</v-card-title>
              <v-card-subtitle>Últimos 12 meses. Toque na legenda para mostrar ou esconder cada série.</v-card-subtitle>
            </v-card-item>
            <v-card-text>
              <div class="chart" role="img" :aria-label="chartDescription">
                <Bar :data="chartData" :options="chartOptions" />
              </div>
            </v-card-text>
          </v-card>
        </v-col>

        <v-col cols="4" md="7">
          <v-card class="h-100">
            <v-card-item>
              <v-card-title class="text-title-large">
                {{ rainfall.stations.length ? 'Onde a chuva foi medida' : 'Onde a chuva foi estimada' }}
              </v-card-title>
              <v-card-subtitle>
                {{ rainfall.stations.length ? 'Pluviômetros do CEMADEN na cidade' : 'Ponto usado para a estimativa' }}
              </v-card-subtitle>
            </v-card-item>
            <v-card-text>
              <v-sheet rounded="lg" class="overflow-hidden" border>
                <BaseMap height="320px" description="Mapa com os pluviômetros e o centro das ocorrências" @ready="onMapReady" />
              </v-sheet>
              <v-list v-if="rainfall.stations.length" class="pa-0 mt-2">
                <v-list-item
                  v-for="station in rainfall.stations.slice(0, 5)"
                  :key="station.code"
                  :title="station.name"
                  :subtitle="`${station.distanceKm} km do centro das ocorrências · ${station.daysWithData} dias com dado`"
                  :prepend-icon="station.code === rainfall.measured?.station?.code ? 'mdi-check-circle' : 'mdi-water-outline'"
                  class="px-0"
                />
              </v-list>
            </v-card-text>
          </v-card>
        </v-col>

        <v-col cols="4" md="5">
          <v-card class="h-100" color="surface-variant" elevation="0">
            <v-card-item prepend-icon="mdi-information-outline">
              <v-card-title class="text-title-large">Medição e estimativa</v-card-title>
            </v-card-item>
            <v-card-text class="text-body-large">
              <p>
                <strong>Medida</strong>: chuva registrada por um pluviômetro do CEMADEN, o mais próximo do centro das
                ocorrências da cidade. Mostra o que caiu naquele ponto.
              </p>
              <p class="mt-3">
                <strong>Estimada</strong>: cálculo do modelo climático ERA5 feito pelo Open-Meteo para qualquer lugar,
                mesmo sem estação. É útil para comparar meses, mas pode errar em temporais localizados.
              </p>
              <p class="mt-3">
                As duas séries aparecem lado a lado e nunca são misturadas numa média.
              </p>
              <p class="mt-3 text-body-small">
                Fontes: medições da rede de pluviômetros do CEMADEN; estimativas de
                <a href="https://open-meteo.com/" target="_blank" rel="noopener">Open-Meteo.com</a> (ERA5, licença
                CC BY 4.0); códigos de municípios do IBGE.
              </p>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
    </template>
  </div>
</template>

<style scoped>
.chart {
  position: relative;
  height: 340px;
}
</style>
