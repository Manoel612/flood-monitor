<script setup>
defineProps({
  label: { type: String, required: true },
  value: { type: [String, Number], default: '–' },
  icon: { type: String, default: null }
})
</script>

<template>
  <v-card class="h-100">
    <v-card-text class="d-flex align-start ga-3">
      <v-avatar v-if="icon" color="primary-container" size="40" rounded="lg">
        <v-icon :icon="icon" size="22" />
      </v-avatar>
      <div class="flex-grow-1 texts">
        <div class="text-label-large text-medium-emphasis">{{ label }}</div>

        <div :class="typeof value === 'number' ? 'text-headline-medium' : 'text-title-large'" class="value">
          {{ value }}
        </div>
      </div>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.texts {
  min-width: 0;
}

.value {
  overflow-wrap: break-word;
  hyphens: auto;
}
</style>
