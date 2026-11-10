<script setup>
defineProps({
  options: { type: Array, required: true },
  label: { type: String, required: true }
})

const selected = defineModel({ required: true })
</script>

<template>
  <v-btn-toggle
    v-model="selected"
    mandatory
    variant="text"
    selected-class="bg-primary-container"
    class="border bg-surface"
    :aria-label="label"
  >
    <v-btn v-for="option in options" :key="option.value" :value="option.value" class="segment">
      <v-icon v-if="selected === option.value" icon="mdi-check" size="18" start />
      {{ option.label }}
    </v-btn>
  </v-btn-toggle>
</template>

<style scoped>
.segment {
  padding-inline: 12px;
  text-transform: none;
  letter-spacing: normal;
}
</style>
