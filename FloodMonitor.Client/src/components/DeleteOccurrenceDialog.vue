<script setup>
import { computed, ref, watch } from 'vue'
import { deleteOccurrence } from '@/services/api'
import { placeName } from '@/utils/location'
import { formatDateTime } from '@/utils/formatters'

const props = defineProps({
  occurrence: { type: Object, default: null }
})

const emit = defineEmits(['deleted'])

const open = defineModel({ type: Boolean, default: false })
const deleting = ref(false)
const error = ref('')

const description = computed(() => {
  if (!props.occurrence) return ''
  const place = placeName(props.occurrence) || 'local não identificado'
  return `${place}, em ${formatDateTime(props.occurrence.occurredAt)}`
})

watch(open, (isOpen) => {
  if (isOpen) error.value = ''
})

async function confirmDelete() {
  deleting.value = true
  error.value = ''
  try {
    await deleteOccurrence(props.occurrence.id)
    open.value = false
    emit('deleted', props.occurrence.id)
  } catch (e) {
    error.value = e.message
  } finally {
    deleting.value = false
  }
}
</script>

<template>
  <v-dialog v-model="open" max-width="440" :persistent="deleting">
    <v-card rounded="lg">
      <v-card-item prepend-icon="mdi-delete-outline">
        <v-card-title class="text-title-large">Excluir ocorrência?</v-card-title>
      </v-card-item>
      <v-card-text class="text-body-large">
        <p v-if="occurrence?.photoUrl">A ocorrência de <strong>{{ description }}</strong> e a foto dela serão apagadas de vez.</p>
        <p v-else>A ocorrência de <strong>{{ description }}</strong> será apagada de vez.</p>
        <p class="mt-2">Isso não pode ser desfeito.</p>
        <v-alert v-if="error" type="error" color="error-container" class="mt-4" role="alert">{{ error }}</v-alert>
      </v-card-text>
      <v-card-actions class="px-4 pb-4">
        <v-spacer />
        <v-btn variant="text" color="primary" :disabled="deleting" @click="open = false">Cancelar</v-btn>
        <v-btn variant="flat" color="error" :loading="deleting" @click="confirmDelete">Excluir</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
