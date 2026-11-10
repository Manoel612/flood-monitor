<script setup>
import { WATER_LEVELS } from '@/utils/waterLevels'

const waterLevel = defineModel({ type: String, default: null })
</script>

<template>
  <v-row class="layout-grid" role="group" aria-label="Altura da água">
    <v-col v-for="level in WATER_LEVELS" :key="level.value" cols="2" md="6">
      <v-btn
        block
        rounded="lg"
        height="88"
        :variant="waterLevel === level.value ? 'flat' : 'outlined'"
        :color="waterLevel === level.value ? level.color : undefined"
        :style="waterLevel === level.value ? { color: level.textColor } : undefined"
        :aria-pressed="waterLevel === level.value"
        class="option"
        @click="waterLevel = level.value"
      >
        <span class="option-content">
          <v-icon
            :icon="waterLevel === level.value ? 'mdi-check-circle' : level.icon"
            :color="waterLevel === level.value ? undefined : level.color"
            size="32"
          />
          <span class="text-label-large">{{ level.label }}</span>
        </span>
      </v-btn>
    </v-col>
  </v-row>
</template>

<style scoped>
.option {
  text-transform: none;
  letter-spacing: normal;
}

.option-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  white-space: normal;
}
</style>
