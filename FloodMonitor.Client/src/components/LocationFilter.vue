<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import SegmentedButtons from '@/components/SegmentedButtons.vue'
import { getPlaces, searchAddress } from '@/services/api'
import { formatRadius, placeName } from '@/utils/location'

defineProps({
  title: { type: String, default: 'Onde?' }
})

const filter = defineModel({ type: Object, default: null })

const RADIUS_OPTIONS = [500, 1000, 2000].map((meters) => ({ value: meters, label: formatRadius(meters) }))
const PLACE_TYPE_LABELS = { state: 'Estado', city: 'Cidade', neighborhood: 'Bairro' }

const places = ref({ states: [], cities: [], neighborhoods: [] })
const filterType = ref('all')
const selectedPlace = ref(null)
const address = ref(null)
const radius = ref(1000)

const searchText = ref('')
const searchResults = ref([])
const searching = ref(false)
const searchError = ref('')

const filterTypes = computed(() => [
  { title: 'Todos os lugares', value: 'all' },
  { title: 'Estado', value: 'state', props: { disabled: !places.value.states.length } },
  { title: 'Cidade', value: 'city', props: { disabled: !places.value.cities.length } },
  { title: 'Bairro', value: 'neighborhood', props: { disabled: !places.value.neighborhoods.length } },
  { title: 'Endereço (raio)', value: 'address' }
])

const isPlaceFilter = computed(() => filterType.value in PLACE_TYPE_LABELS)

const placeOptions = computed(() => {
  const list = {
    state: places.value.states,
    city: places.value.cities,
    neighborhood: places.value.neighborhoods
  }[filterType.value] ?? []
  return list.map((place) => ({
    title: `${placeName(place)} (${place.total})`,
    value: [place.state, place.city, place.neighborhood].join('|'),
    place
  }))
})

const summary = computed(() => {
  if (filterType.value === 'address') {
    return address.value ? `Até ${formatRadius(radius.value)} de ${address.value.label}` : 'Busque um endereço'
  }
  if (isPlaceFilter.value) {
    const option = placeOptions.value.find((o) => o.value === selectedPlace.value)
    const article = filterType.value === 'city' ? 'uma' : 'um'
    return option ? placeName(option.place) : `Escolha ${article} ${PLACE_TYPE_LABELS[filterType.value].toLowerCase()}`
  }
  return 'Todos os lugares'
})

onMounted(async () => {
  try {
    places.value = await getPlaces()
  } catch {
  }
})

async function search() {
  searchError.value = ''
  searchResults.value = []
  const query = searchText.value.trim()
  if (query.length < 3) {
    searchError.value = 'Digite pelo menos 3 letras do endereço.'
    return
  }
  searching.value = true
  try {
    searchResults.value = await searchAddress(query)
    if (!searchResults.value.length) searchError.value = 'Nenhum endereço encontrado. Tente incluir o bairro ou a cidade.'
  } catch (e) {
    searchError.value = e.message
  } finally {
    searching.value = false
  }
}

function chooseAddress(result) {
  address.value = result
  searchResults.value = []
  searchText.value = ''
}

watch(filterType, () => {
  selectedPlace.value = null
  address.value = null
  searchResults.value = []
  searchError.value = ''
}, { flush: 'sync' })

watch([filterType, selectedPlace, address, radius], () => {
  if (filterType.value === 'address') {
    filter.value = address.value
      ? { type: 'address', label: address.value.label, lat: address.value.lat, lon: address.value.lon, radius: radius.value }
      : null
    return
  }
  const option = placeOptions.value.find((o) => o.value === selectedPlace.value)
  if (!option) {
    filter.value = null
    return
  }
  const { state, city, neighborhood } = option.place
  filter.value =
    filterType.value === 'state' ? { type: 'state', state }
      : filterType.value === 'city' ? { type: 'city', city, state }
        : { type: 'neighborhood', neighborhood, city, state }
})
</script>

<template>
  <v-card>
    <v-card-item prepend-icon="mdi-filter-variant">
      <v-card-title class="text-title-large">{{ title }}</v-card-title>
      <v-card-subtitle aria-live="polite">{{ summary }}</v-card-subtitle>
    </v-card-item>

    <v-card-text>
      <slot />

      <v-row class="layout-grid">
        <v-col cols="4" md="4">
          <v-select
            v-model="filterType"
            :items="filterTypes"
            label="Local"
            prepend-inner-icon="mdi-map-marker-outline"
            hide-details
          />
        </v-col>

        <v-col v-if="isPlaceFilter" cols="4" md="8">
          <v-autocomplete
            v-model="selectedPlace"
            :items="placeOptions"
            :label="PLACE_TYPE_LABELS[filterType]"
            no-data-text="Nenhum lugar com registros"
            auto-select-first
            clearable
            hide-details
          />
        </v-col>

        <v-col v-else-if="filterType === 'address'" cols="4" md="8">
          <form class="d-flex ga-2 align-start" role="search" @submit.prevent="search">
            <v-text-field
              v-model="searchText"
              label="Endereço"
              placeholder="Ex.: Rua Marechal Floriano Peixoto, 1000"
              prepend-inner-icon="mdi-magnify"
              hint="A busca usa o OpenStreetMap."
              persistent-hint
              class="flex-grow-1"
            />
            <tonal-button type="submit" height="48" :loading="searching">Buscar</tonal-button>
          </form>
        </v-col>
      </v-row>

      <template v-if="filterType === 'address'">
        <v-alert v-if="searchError" type="warning" class="mt-4" role="alert">{{ searchError }}</v-alert>

        <v-list v-if="searchResults.length" class="mt-4 pa-0 border rounded-lg" aria-label="Endereços encontrados">
          <v-list-item
            v-for="(result, index) in searchResults"
            :key="index"
            :title="result.label"
            prepend-icon="mdi-map-marker-outline"
            @click="chooseAddress(result)"
          />
        </v-list>

        <div v-if="address" class="mt-4">
          <div class="text-body-large mb-2">
            Raio em volta de <strong>{{ address.label }}</strong>
          </div>
          <SegmentedButtons v-model="radius" :options="RADIUS_OPTIONS" label="Raio" />
        </div>
      </template>
    </v-card-text>
  </v-card>
</template>
