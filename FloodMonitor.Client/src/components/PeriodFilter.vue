<script setup>
import SegmentedButtons from '@/components/SegmentedButtons.vue'
import { PERIODS } from '@/utils/formatters'

const period = defineModel({ type: String, required: true })
</script>

<template>
  <SegmentedButtons v-model="period" :options="PERIODS" label="Período" />
</template>
