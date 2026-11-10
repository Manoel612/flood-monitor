<script setup>
import { ref } from 'vue'
import PageHeader from '@/components/PageHeader.vue'
import StepCard from '@/components/StepCard.vue'
import { submitFeedback } from '@/services/api'

const PROFILES = [
  'Moro em área que alaga',
  'Moro em outra região',
  'Trabalho na Defesa Civil ou na prefeitura',
  'Estudante ou pesquisador',
  'Outro'
]

const rating = ref(0)
const profile = ref(null)
const comment = ref('')
const honeypot = ref('')
const submitting = ref(false)
const submitted = ref(false)
const error = ref('')

async function submit() {
  error.value = ''
  if (!rating.value) {
    error.value = 'Escolha de 1 a 5 estrelas.'
    return
  }
  submitting.value = true
  try {
    await submitFeedback({
      rating: rating.value,
      profile: profile.value,
      comment: comment.value.trim() || null,
      site: honeypot.value
    })
    submitted.value = true
  } catch (e) {
    error.value = e.message
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="page-content">
    <v-row class="layout-grid justify-center">
      <v-col cols="4" md="8" lg="6">
        <v-card v-if="submitted" class="text-center pa-6">
          <v-avatar color="tertiary-container" size="80">
            <v-icon icon="mdi-heart-outline" size="44" />
          </v-avatar>
          <h1 class="text-headline-small mt-4">Obrigado pela avaliação!</h1>
          <p class="text-body-large text-medium-emphasis mt-2">Sua opinião ajuda a melhorar o aplicativo.</p>
          <primary-button block class="mt-6" :to="{ name: 'map' }">Voltar ao mapa</primary-button>
        </v-card>

        <form v-else novalidate @submit.prevent="submit">
          <PageHeader title="Avalie o aplicativo" subtitle="Leva 30 segundos. A avaliação é anônima." />

          <StepCard :step="1" title="Foi fácil usar o Monitor de Enchentes?">
            <div class="text-center">
              <v-rating v-model="rating" length="5" size="x-large" hover />
              <div class="text-body-large text-medium-emphasis">1 = muito difícil · 5 = muito fácil</div>
            </div>
          </StepCard>

          <StepCard :step="2" title="Quem é você?" optional>
            <v-radio-group v-model="profile" hide-details>
              <v-radio v-for="option in PROFILES" :key="option" :label="option" :value="option" />
            </v-radio-group>
          </StepCard>

          <StepCard :step="3" title="Quer deixar um comentário?" optional>
            <v-textarea
              v-model="comment"
              label="O que achou? O que podemos melhorar?"
              counter="500"
              maxlength="500"
              rows="3"
              auto-grow
            />
          </StepCard>

          <div class="honeypot" aria-hidden="true">
            <label>Site <input v-model="honeypot" type="text" name="site" tabindex="-1" autocomplete="off" /></label>
          </div>

          <v-alert v-if="error" type="warning" class="section" role="alert">{{ error }}</v-alert>

          <primary-button type="submit" size="x-large" block prepend-icon="mdi-send" :loading="submitting" class="section">
            Enviar avaliação
          </primary-button>
        </form>
      </v-col>
    </v-row>
  </div>
</template>
