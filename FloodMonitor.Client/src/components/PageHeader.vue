<script setup>
defineProps({
  title: { type: String, required: true },
  subtitle: { type: String, default: null }
})
</script>

<template>
  <header class="page-header section">
    <div class="texts">
      <h1 class="text-headline-small">{{ title }}</h1>
      <p v-if="subtitle" class="text-body-large text-medium-emphasis mt-1">{{ subtitle }}</p>
    </div>
    <div v-if="$slots.actions" class="actions">
      <slot name="actions" />
    </div>
  </header>
</template>

<style scoped>
.page-header {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
  padding-top: 8px;
}

.texts {
  min-width: 0;
}

.texts p {
  margin-bottom: 0;
}
</style>
