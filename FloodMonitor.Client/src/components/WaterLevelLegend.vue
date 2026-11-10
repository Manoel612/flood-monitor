<script setup>
import { ref } from 'vue'
import { WATER_LEVELS } from '@/utils/waterLevels'

const props = defineProps({
  title: { type: String, default: 'Altura da água' },
  collapsible: { type: Boolean, default: false },
  initiallyOpen: { type: Boolean, default: true }
})

const open = ref(!props.collapsible || props.initiallyOpen)
</script>

<template>
  <v-sheet rounded="lg" elevation="3" class="legend">
    <button
      v-if="collapsible"
      type="button"
      class="legend-header text-label-large"
      :aria-expanded="open"
      aria-controls="water-level-list"
      @click="open = !open"
    >
      <span>{{ title }}</span>
      <v-icon :icon="open ? 'mdi-chevron-up' : 'mdi-chevron-down'" size="20" />
    </button>
    <div v-else class="legend-header text-label-large">{{ title }}</div>

    <v-expand-transition>
      <ul v-show="open" id="water-level-list" class="level-list">
        <li v-for="level in WATER_LEVELS" :key="level.value" class="text-body-small">
          <span class="dot" :style="{ backgroundColor: level.color }" aria-hidden="true" />
          {{ level.label }}
        </li>
      </ul>
    </v-expand-transition>
  </v-sheet>
</template>

<style scoped>
.legend-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
  min-height: 40px;
  padding: 8px 12px;
  color: inherit;
  text-align: start;
  border-radius: inherit;
}

button.legend-header {
  appearance: none;
  background: none;
  border: 0;
  font: inherit;
  cursor: pointer;
}

button.legend-header:hover {
  background: rgba(var(--v-theme-on-surface), var(--v-hover-opacity));
}

button.legend-header:focus-visible {
  outline: 2px solid rgb(var(--v-theme-primary));
  outline-offset: -2px;
}

.level-list {
  list-style: none;
  padding: 0 12px 12px;
  margin: 0;
}

.level-list li {
  display: flex;
  align-items: center;
  gap: 8px;
  line-height: 1.6;
}

.dot {
  width: 14px;
  height: 14px;
  border-radius: 50%;
  border: 2px solid rgb(var(--v-theme-on-surface));
  flex-shrink: 0;
}
</style>
