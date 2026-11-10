<script setup>
import { computed } from 'vue'
import { waterLevelByValue } from '@/utils/waterLevels'

const props = defineProps({
  waterLevel: { type: String, required: true }
})

const level = computed(() => waterLevelByValue(props.waterLevel))
</script>

<template>
  <v-chip variant="flat" :color="level.color" :style="{ color: level.textColor }" :prepend-icon="level.icon">
    Água {{ level.label.toLowerCase() }}
  </v-chip>
</template>
