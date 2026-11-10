<script setup>
import { computed, onBeforeUnmount, ref } from 'vue'
import L from 'leaflet'
import BaseMap from '@/components/BaseMap.vue'
import PageHeader from '@/components/PageHeader.vue'
import StepCard from '@/components/StepCard.vue'
import WaterLevelPicker from '@/components/WaterLevelPicker.vue'
import { reportOccurrence } from '@/services/api'
import { shrinkPhotoWithoutMetadata } from '@/utils/image'
import { nowForLocalInput } from '@/utils/formatters'

const position = ref(null)
const positionSource = ref('')
const locatingByGps = ref(false)
const locationWarning = ref('')
const waterLevel = ref(null)
const when = ref('now')
const occurredAt = ref('')
const description = ref('')
const photo = ref(null)
const photoPreview = ref('')
const processingPhoto = ref(false)
const photoInput = ref(null)
const honeypot = ref('')
const submitting = ref(false)
const triedToSubmit = ref(false)
const error = ref('')
const reported = ref(null)

const maxLocalDateTime = nowForLocalInput()

const missingFields = computed(() => {
  const missing = []
  if (!position.value) missing.push('marque o local no mapa')
  if (!waterLevel.value) missing.push('escolha a altura da água')
  if (when.value === 'other' && !occurredAt.value) missing.push('informe a data e a hora')
  return missing
})

let map = null
let pin = null

const pinIcon = L.divIcon({
  className: 'map-pin',
  html: '<span class="mdi mdi-map-marker"></span>',
  iconSize: [48, 48],
  iconAnchor: [24, 46]
})

function onMapReady(instance) {
  map = instance
  pin = null
  map.on('click', (event) => setPosition(event.latlng, 'map'))
}

function setPosition(latlng, source) {
  position.value = { lat: latlng.lat, lng: latlng.lng }
  positionSource.value = source
  if (pin) {
    pin.setLatLng(latlng)
    return
  }
  pin = L.marker(latlng, { draggable: true, icon: pinIcon, title: 'Local da enchente', alt: 'Local da enchente' }).addTo(map)
  pin.on('dragend', () => setPosition(pin.getLatLng(), 'map'))
}

function useGps() {
  locationWarning.value = ''

  if (!('geolocation' in navigator) || !window.isSecureContext) {
    locationWarning.value = 'O navegador não liberou o GPS nesta página. Toque no mapa para marcar o local.'
    return
  }
  locatingByGps.value = true
  navigator.geolocation.getCurrentPosition(
    (result) => {
      locatingByGps.value = false
      const latlng = L.latLng(result.coords.latitude, result.coords.longitude)
      setPosition(latlng, 'gps')
      map.setView(latlng, 17)
    },
    () => {
      locatingByGps.value = false
      locationWarning.value = 'Não foi possível obter sua localização. Toque no mapa para marcar o local.'
    },
    { enableHighAccuracy: true, timeout: 15000, maximumAge: 60000 }
  )
}

async function onPhotoSelected(event) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return

  processingPhoto.value = true
  try {
    const shrunk = await shrinkPhotoWithoutMetadata(file)
    removePhoto()
    photo.value = shrunk
    photoPreview.value = URL.createObjectURL(shrunk)
  } catch {
    error.value = 'Não conseguimos usar essa foto. Tente outra imagem.'
  } finally {
    processingPhoto.value = false
  }
}

function removePhoto() {
  if (photoPreview.value) URL.revokeObjectURL(photoPreview.value)
  photoPreview.value = ''
  photo.value = null
}

onBeforeUnmount(removePhoto)

async function submit() {
  triedToSubmit.value = true
  error.value = ''
  if (missingFields.value.length) return

  const formData = new FormData()
  formData.append('latitude', position.value.lat.toFixed(6))
  formData.append('longitude', position.value.lng.toFixed(6))
  formData.append('waterLevel', waterLevel.value)
  if (when.value === 'other') formData.append('occurredAt', new Date(occurredAt.value).toISOString())
  if (description.value.trim()) formData.append('description', description.value.trim())
  if (photo.value) formData.append('photo', photo.value)
  formData.append('site', honeypot.value)

  submitting.value = true
  try {
    reported.value = await reportOccurrence(formData)
    removePhoto()
    window.scrollTo({ top: 0 })
  } catch (e) {
    error.value = e.message
  } finally {
    submitting.value = false
  }
}

function startOver() {
  position.value = null
  positionSource.value = ''
  locationWarning.value = ''
  waterLevel.value = null
  when.value = 'now'
  occurredAt.value = ''
  description.value = ''
  removePhoto()
  triedToSubmit.value = false
  error.value = ''
  reported.value = null
}
</script>

<template>
  <div class="page-content">
    <v-row class="layout-grid justify-center">
      <v-col cols="4" md="8" lg="6">
        <v-card v-if="reported" class="text-center pa-6">
          <v-avatar color="tertiary-container" size="80">
            <v-icon icon="mdi-check" size="48" />
          </v-avatar>
          <h1 class="text-headline-small mt-4">Obrigado! Ocorrência registrada.</h1>
          <p class="text-body-large text-medium-emphasis mt-2">
            Seu registro já aparece no mapa e ajuda a mostrar as áreas de risco da cidade.
          </p>
          <div class="d-flex flex-column ga-3 mt-6">
            <primary-button block :to="{ name: 'occurrence', params: { id: reported.id } }">
              Ver meu registro
            </primary-button>
            <tonal-button block prepend-icon="mdi-star-outline" :to="{ name: 'feedback' }">
              Avaliar o aplicativo
            </tonal-button>
            <v-btn variant="text" color="primary" size="large" @click="startOver">Registrar outra ocorrência</v-btn>
          </div>
        </v-card>

        <form v-else novalidate @submit.prevent="submit">
          <PageHeader title="Registrar enchente" subtitle="Leva menos de um minuto e não precisa de cadastro." />

          <StepCard :step="1" title="Onde está alagado?">
            <tonal-button block prepend-icon="mdi-crosshairs-gps" :loading="locatingByGps" @click="useGps">
              Usar minha localização
            </tonal-button>
            <p class="text-body-large my-3">Ou toque no mapa. Dá para arrastar o marcador para ajustar.</p>
            <v-sheet rounded="lg" class="overflow-hidden" border>
              <BaseMap height="300px" description="Mapa para marcar o local da enchente" @ready="onMapReady" />
            </v-sheet>
            <v-alert v-if="locationWarning" type="info" color="surface-variant" class="mt-3">{{ locationWarning }}</v-alert>
            <p v-if="position" class="text-body-large mt-3 d-flex align-center ga-1">
              <v-icon icon="mdi-check-circle" color="success" />
              Local marcado {{ positionSource === 'gps' ? 'pelo GPS' : 'no mapa' }}.
            </p>
          </StepCard>

          <StepCard :step="2" title="Até onde chegou a água?">
            <WaterLevelPicker v-model="waterLevel" />
          </StepCard>

          <StepCard :step="3" title="Quando aconteceu?">
            <v-radio-group v-model="when" hide-details>
              <v-radio label="Está acontecendo agora" value="now" />
              <v-radio label="Em outro momento" value="other" />
            </v-radio-group>
            <v-text-field
              v-if="when === 'other'"
              v-model="occurredAt"
              type="datetime-local"
              label="Data e hora"
              :max="maxLocalDateTime"
              class="mt-3"
            />
          </StepCard>

          <StepCard :step="4" title="Quer contar mais?" optional>
            <v-textarea
              v-model="description"
              label="O que está acontecendo?"
              placeholder="Ex.: a rua encheu depois da chuva e a água entrou nas casas"
              counter="500"
              maxlength="500"
              rows="3"
              auto-grow
            />
            <input ref="photoInput" type="file" accept="image/*" class="d-none" @change="onPhotoSelected" />
            <v-img v-if="photoPreview" :src="photoPreview" max-height="240" class="rounded-lg mb-3" alt="Prévia da foto escolhida" />
            <div class="d-flex flex-wrap ga-2">
              <tonal-button prepend-icon="mdi-camera" :loading="processingPhoto" @click="photoInput.click()">
                {{ photo ? 'Trocar foto' : 'Tirar ou escolher foto' }}
              </tonal-button>
              <v-btn v-if="photo" variant="text" color="primary" size="large" prepend-icon="mdi-delete-outline" @click="removePhoto">
                Remover foto
              </v-btn>
            </div>
            <v-sheet color="surface-variant" rounded="lg" class="d-flex ga-2 pa-3 mt-4 text-body-medium">
              <v-icon icon="mdi-shield-account-outline" size="20" />
              <span>
                Evite fotografar rostos ou placas de carro. Os dados escondidos da foto (como a localização exata e
                o modelo do celular) são apagados antes do envio.
              </span>
            </v-sheet>
          </StepCard>

          <div class="honeypot" aria-hidden="true">
            <label>Site <input v-model="honeypot" type="text" name="site" tabindex="-1" autocomplete="off" /></label>
          </div>

          <v-alert v-if="triedToSubmit && missingFields.length" type="warning" class="section" role="alert">
            Falta pouco: {{ missingFields.join(' e ') }}.
          </v-alert>
          <v-alert v-if="error" type="error" color="error-container" class="section" role="alert">{{ error }}</v-alert>

          <primary-button type="submit" size="x-large" block prepend-icon="mdi-send" :loading="submitting" class="section">
            Enviar registro
          </primary-button>
        </form>
      </v-col>
    </v-row>
  </div>
</template>
