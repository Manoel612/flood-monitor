<script setup>
defineProps({
  step: { type: Number, required: true },
  title: { type: String, required: true },
  optional: { type: Boolean, default: false }
})
</script>

<template>
  <v-card class="section">
    <v-card-item>
      <template #prepend>
        <v-avatar color="primary-container" size="32" aria-hidden="true">
          <span class="text-label-large">{{ step }}</span>
        </v-avatar>
      </template>
      <v-card-title class="text-title-large">
        {{ title }}
        <span v-if="optional" class="text-body-medium text-medium-emphasis">(opcional)</span>
      </v-card-title>
    </v-card-item>
    <v-card-text>
      <slot />
    </v-card-text>
  </v-card>
</template>
