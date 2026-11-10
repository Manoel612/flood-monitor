<script setup>
import { onBeforeUnmount, onMounted, ref } from 'vue'
import L from 'leaflet'
import { MAP } from '@/config'

const props = defineProps({
  center: { type: Array, default: () => MAP.center },
  zoom: { type: Number, default: MAP.zoom },
  height: { type: String, default: '100%' },
  description: { type: String, default: 'Mapa' }
})

const emit = defineEmits(['ready'])

const container = ref(null)
let map = null
let resizeObserver = null

onMounted(() => {
  map = L.map(container.value).setView(props.center, props.zoom)
  L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
  }).addTo(map)

  resizeObserver = new ResizeObserver(() => map.invalidateSize())
  resizeObserver.observe(container.value)

  emit('ready', map)
})

onBeforeUnmount(() => {
  resizeObserver?.disconnect()
  map?.remove()
})
</script>

<template>
  <div ref="container" class="base-map" :style="{ height }" role="region" :aria-label="description" />
</template>

<style scoped>
.base-map {
  width: 100%;
  border-radius: inherit;
  z-index: 0;
}
</style>
