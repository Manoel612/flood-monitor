<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import LocationFilter from '@/components/LocationFilter.vue'
import PageHeader from '@/components/PageHeader.vue'
import PeriodFilter from '@/components/PeriodFilter.vue'
import SegmentedButtons from '@/components/SegmentedButtons.vue'
import WaterLevelChip from '@/components/WaterLevelChip.vue'
import { listRecentOccurrences } from '@/services/api'
import { periodStart } from '@/utils/formatters'
import { cityStateAndDate, locationQuery } from '@/utils/location'

const PAGE_SIZE = 12
const AUTHOR_OPTIONS = [
  { value: 'everyone', label: 'Todas' },
  { value: 'mine', label: 'Enviadas por mim' }
]

const period = ref('12m')
const location = ref(null)
const author = ref('everyone')
const onlyMine = computed(() => author.value === 'mine')
const items = ref([])
const total = ref(0)
const page = ref(0)
const loading = ref(false)
const error = ref('')

const hasMore = computed(() => items.value.length < total.value)
const isFirstLoad = computed(() => loading.value && !items.value.length)

let requestGeneration = 0

async function loadPage(restart = false) {
  if (restart) {
    requestGeneration++
    items.value = []
    total.value = 0
    page.value = 0
  } else if (loading.value) {
    return
  }
  const generation = requestGeneration
  loading.value = true
  error.value = ''
  try {
    const response = await listRecentOccurrences(
      {
        from: periodStart(period.value),
        ...locationQuery(location.value),
        page: page.value + 1,
        pageSize: PAGE_SIZE
      },
      { onlyMine: onlyMine.value }
    )
    if (generation !== requestGeneration) return
    items.value = [...items.value, ...response.items]
    total.value = response.total
    page.value = response.page
  } catch (e) {
    if (generation === requestGeneration) error.value = e.message
  } finally {
    if (generation === requestGeneration) loading.value = false
  }
}

onMounted(() => loadPage(true))
watch([period, location, author], () => loadPage(true))

const listEnd = ref(null)
let intersectionObserver = null

onMounted(() => {
  intersectionObserver = new IntersectionObserver(
    ([entry]) => {
      if (entry.isIntersecting && hasMore.value && !loading.value && !error.value) loadPage()
    },
    { rootMargin: '400px' }
  )
})
onBeforeUnmount(() => intersectionObserver?.disconnect())

watch(listEnd, (element, previous) => {
  if (previous) intersectionObserver?.unobserve(previous)
  if (element) intersectionObserver?.observe(element)
})

watch(loading, async (isLoading) => {
  if (isLoading || !hasMore.value || !listEnd.value) return
  await nextTick()
  const { top } = listEnd.value.getBoundingClientRect()
  if (top < window.innerHeight + 400) loadPage()
})

const emptyState = computed(() =>
  onlyMine.value
    ? {
        title: 'Nenhuma ocorrência sua aqui',
        text: 'Aparecem só as que você registrou neste aparelho e navegador, dentro do período e do local escolhidos.'
      }
    : { title: 'Nenhuma ocorrência encontrada', text: 'Escolha um período maior, outro local ou registre uma enchente que você viu.' }
)
</script>

<template>
  <div class="page-content">
    <PageHeader
      title="Ocorrências registradas"
      subtitle="Relatos enviados pelos moradores, do mais recente para o mais antigo."
    >
      <template #actions>
        <PeriodFilter v-model="period" />
      </template>
    </PageHeader>

    <LocationFilter v-model="location" title="Filtros" class="section">
      <SegmentedButtons v-model="author" :options="AUTHOR_OPTIONS" label="Quem enviou" class="mb-4" />
    </LocationFilter>

    <v-alert v-if="error" type="error" color="error-container" class="section">{{ error }}</v-alert>

    <p v-if="total" class="text-body-large text-medium-emphasis section" aria-live="polite">
      {{ total }} ocorrência{{ total === 1 ? '' : 's' }}{{ onlyMine ? ' enviadas por você' : '' }}
    </p>

    <v-row v-if="isFirstLoad" class="layout-grid section" aria-busy="true">
      <v-col v-for="n in 3" :key="n" cols="4" md="6" lg="4">
        <v-skeleton-loader type="card" class="rounded-lg" />
      </v-col>
    </v-row>

    <v-empty-state
      v-else-if="!loading && !error && !items.length"
      icon="mdi-water-off-outline"
      :title="emptyState.title"
      :text="emptyState.text"
      class="section"
    >
      <template #actions>
        <primary-button prepend-icon="mdi-plus" :to="{ name: 'report' }">Registrar enchente</primary-button>
      </template>
    </v-empty-state>

    <v-row v-else class="layout-grid section">
      <v-col v-for="occurrence in items" :key="occurrence.id" cols="4" md="6" lg="4">
        <v-card :to="{ name: 'occurrence', params: { id: occurrence.id } }" class="h-100 d-flex flex-column">
          <v-img v-if="occurrence.photoUrl" :src="occurrence.photoUrl" height="180" cover alt="Foto enviada com o registro" />
          <v-card-item>
            <WaterLevelChip :water-level="occurrence.waterLevel" size="small" class="mb-2" />
            <v-card-title class="text-title-large">{{ occurrence.neighborhood ?? 'Bairro não identificado' }}</v-card-title>
            <v-card-subtitle>{{ cityStateAndDate(occurrence) }}</v-card-subtitle>
          </v-card-item>
          <v-card-text v-if="occurrence.description" class="text-body-large description">{{ occurrence.description }}</v-card-text>
          <v-spacer />
          <v-card-actions class="px-4 pb-4">
            <span class="text-label-large text-primary">Ver detalhes</span>
            <v-icon icon="mdi-chevron-right" color="primary" size="20" />
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>

    <div v-if="hasMore" ref="listEnd" class="d-flex justify-center py-6 section">
      <v-progress-circular v-if="loading" indeterminate aria-label="Carregando mais ocorrências" />
    </div>
    <p v-else-if="items.length > PAGE_SIZE" class="text-body-medium text-medium-emphasis text-center py-6 section">
      Você chegou ao fim da lista.
    </p>
  </div>
</template>

<style scoped>
.description {
  display: -webkit-box;
  -webkit-line-clamp: 3;
  line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
  padding-bottom: 0;
}
</style>
