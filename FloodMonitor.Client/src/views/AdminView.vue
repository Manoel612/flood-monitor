<script setup>
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { Bar } from 'vue-chartjs'
import { BarElement, CategoryScale, Chart as ChartJS, Legend, LinearScale, Tooltip } from 'chart.js'
import { useTheme } from 'vuetify'
import DeleteOccurrenceDialog from '@/components/DeleteOccurrenceDialog.vue'
import PageHeader from '@/components/PageHeader.vue'
import StatCard from '@/components/StatCard.vue'
import WaterLevelChip from '@/components/WaterLevelChip.vue'
import { downloadEvidenceCsv, getEvidenceSummary, listRecentOccurrences, loginAsAdmin } from '@/services/api'
import { endAdminSession, isAdmin } from '@/services/adminSession'
import { formatDateTime, formatDay } from '@/utils/formatters'
import { cityStateAndDate } from '@/utils/location'

ChartJS.register(BarElement, CategoryScale, Legend, LinearScale, Tooltip)

const CHART_DAYS = 30
const OCCURRENCES_PAGE_SIZE = 10
const SPREADSHEETS = [
  { name: 'visits', fileName: 'acessos.csv', label: 'Acessos' },
  { name: 'occurrences', fileName: 'ocorrencias.csv', label: 'Ocorrências' },
  { name: 'feedback', fileName: 'avaliacoes.csv', label: 'Avaliações' }
]

const router = useRouter()
const theme = useTheme()

const username = ref('')
const password = ref('')
const showPassword = ref(false)
const loggingIn = ref(false)
const loginError = ref('')

async function login() {
  loginError.value = ''
  if (!username.value.trim() || !password.value) {
    loginError.value = 'Informe o usuário e a senha.'
    return
  }
  loggingIn.value = true
  try {
    await loginAsAdmin(username.value.trim(), password.value)
    password.value = ''
  } catch (e) {
    loginError.value = e.message
  } finally {
    loggingIn.value = false
  }
}

function logout() {
  endAdminSession()
  summary.value = null
  occurrences.value = []
}

const summary = ref(null)
const loading = ref(false)
const error = ref('')

async function loadSummary() {
  loading.value = true
  error.value = ''
  try {
    summary.value = await getEvidenceSummary()
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

const pageTitles = Object.fromEntries(router.getRoutes().filter((r) => r.name).map((r) => [r.name, r.meta.title]))
const pageLabel = (page) => pageTitles[page] ?? page

const recentDays = computed(() => (summary.value?.byDay ?? []).slice(0, CHART_DAYS).reverse())

const chartData = computed(() => {
  const { colors } = theme.current.value
  return {
    labels: recentDays.value.map((d) => formatDay(d.day).slice(0, 5)),
    datasets: [
      { label: 'Acessos', data: recentDays.value.map((d) => d.visits), backgroundColor: colors.primary },
      { label: 'Visitantes', data: recentDays.value.map((d) => d.sessions), backgroundColor: colors.secondary }
    ]
  }
})

const chartOptions = computed(() => {
  const { colors, dark } = theme.current.value
  const textColor = colors['on-surface']
  const axis = { ticks: { color: textColor }, grid: { color: dark ? 'rgba(255,255,255,0.12)' : 'rgba(0,0,0,0.08)' } }
  return {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false },
    plugins: {
      legend: {
        position: 'bottom',
        labels: { color: textColor, usePointStyle: true, pointStyle: 'circle', boxWidth: 10, boxHeight: 10 }
      }
    },
    scales: { x: axis, y: { ...axis, beginAtZero: true, ticks: { ...axis.ticks, precision: 0 } } }
  }
})

const chartDescription = computed(() => {
  const totalVisits = recentDays.value.reduce((sum, d) => sum + d.visits, 0)
  return `Gráfico de barras com acessos e visitantes por dia. Total de ${totalVisits} acessos nos dias mostrados.`
})

const downloading = ref('')
async function download(spreadsheet) {
  downloading.value = spreadsheet.name
  error.value = ''
  try {
    await downloadEvidenceCsv(spreadsheet.name, spreadsheet.fileName)
  } catch (e) {
    error.value = e.message
  } finally {
    downloading.value = ''
  }
}

const occurrences = ref([])
const occurrencesTotal = ref(0)
const occurrencesPage = ref(0)
const loadingOccurrences = ref(false)

async function loadOccurrences(restart = false) {
  if (restart) {
    occurrences.value = []
    occurrencesPage.value = 0
  }
  loadingOccurrences.value = true
  try {
    const response = await listRecentOccurrences({ page: occurrencesPage.value + 1, pageSize: OCCURRENCES_PAGE_SIZE })
    occurrences.value = [...occurrences.value, ...response.items]
    occurrencesTotal.value = response.total
    occurrencesPage.value = response.page
  } catch (e) {
    error.value = e.message
  } finally {
    loadingOccurrences.value = false
  }
}

const occurrenceToDelete = ref(null)
const deleteDialogOpen = ref(false)
const deletedMessage = ref('')

function askToDelete(occurrence) {
  occurrenceToDelete.value = occurrence
  deleteDialogOpen.value = true
}

function onDeleted(id) {
  occurrences.value = occurrences.value.filter((o) => o.id !== id)
  occurrencesTotal.value--
  deletedMessage.value = 'Ocorrência excluída.'
  loadSummary()
}

watch(isAdmin, (loggedIn) => {
  if (!loggedIn) return
  loadSummary()
  loadOccurrences(true)
}, { immediate: true })
</script>

<template>
  <div class="page-content">
    <v-row v-if="!isAdmin" class="layout-grid justify-center">
      <v-col cols="4" md="6" lg="4">
        <PageHeader title="Área administrativa" subtitle="Entre para ver os relatórios de acesso e gerenciar as ocorrências." />

        <v-card class="section">
          <v-card-text>
            <form novalidate @submit.prevent="login">
              <v-text-field
                v-model="username"
                label="Usuário"
                autocomplete="username"
                prepend-inner-icon="mdi-account-outline"
                class="mb-2"
              />
              <v-text-field
                v-model="password"
                :type="showPassword ? 'text' : 'password'"
                label="Senha"
                autocomplete="current-password"
                prepend-inner-icon="mdi-lock-outline"
                :append-inner-icon="showPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
                @click:append-inner="showPassword = !showPassword"
              />
              <v-alert v-if="loginError" type="warning" class="mt-2 mb-4" role="alert">{{ loginError }}</v-alert>
              <primary-button type="submit" block prepend-icon="mdi-login" :loading="loggingIn" class="mt-2">
                Entrar
              </primary-button>
            </form>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <template v-else>
      <PageHeader title="Área administrativa" subtitle="Relatórios de acesso do site e gestão das ocorrências.">
        <template #actions>
          <v-btn variant="text" color="primary" size="large" prepend-icon="mdi-refresh" :loading="loading" @click="loadSummary">
            Atualizar
          </v-btn>
          <tonal-button prepend-icon="mdi-logout" @click="logout">Sair</tonal-button>
        </template>
      </PageHeader>

      <v-alert v-if="error" type="error" color="error-container" class="section" role="alert">{{ error }}</v-alert>
      <v-progress-linear v-if="loading && !summary" indeterminate class="section" />

      <v-row v-if="summary" class="layout-grid section">
        <v-col cols="2" md="3">
          <StatCard label="Acessos" :value="summary.totalVisits" icon="mdi-eye-outline" />
        </v-col>
        <v-col cols="2" md="3">
          <StatCard label="Visitantes" :value="summary.uniqueSessions" icon="mdi-account-multiple-outline" />
        </v-col>
        <v-col cols="2" md="3">
          <StatCard label="Ocorrências registradas" :value="summary.communityOccurrences" icon="mdi-map-marker-plus-outline" />
        </v-col>
        <v-col cols="2" md="3">
          <StatCard
            label="Avaliações"
            :value="summary.averageRating ? `${summary.totalFeedback} · nota ${summary.averageRating}` : summary.totalFeedback"
            icon="mdi-star-outline"
          />
        </v-col>

        <v-col cols="4" md="8">
          <v-card class="h-100">
            <v-card-item>
              <v-card-title class="text-title-large">Acessos por dia</v-card-title>
              <v-card-subtitle>Últimos {{ CHART_DAYS }} dias com atividade</v-card-subtitle>
            </v-card-item>
            <v-card-text>
              <p v-if="!recentDays.length" class="text-body-medium">Nenhum acesso registrado ainda.</p>
              <div v-else class="chart" role="img" :aria-label="chartDescription">
                <Bar :data="chartData" :options="chartOptions" />
              </div>
            </v-card-text>
          </v-card>
        </v-col>

        <v-col cols="4" md="4">
          <v-card class="h-100">
            <v-card-item>
              <v-card-title class="text-title-large">Páginas mais visitadas</v-card-title>
            </v-card-item>
            <v-card-text>
              <p v-if="!summary.byPage.length" class="text-body-medium">Nenhum acesso registrado ainda.</p>
              <v-table v-else>
                <tbody>
                  <tr v-for="row in summary.byPage" :key="row.page">
                    <td>{{ pageLabel(row.page) }}</td>
                    <td class="text-end">{{ row.visits }}</td>
                  </tr>
                </tbody>
              </v-table>
            </v-card-text>
          </v-card>
        </v-col>

        <v-col cols="4" md="12">
          <v-card>
            <v-card-item>
              <v-card-title class="text-title-large">Atividade por dia</v-card-title>
            </v-card-item>
            <v-card-text>
              <p v-if="!summary.byDay.length" class="text-body-medium">Nenhuma atividade registrada ainda.</p>
              <div v-else class="table-scroll">
                <v-table>
                  <thead>
                    <tr>
                      <th scope="col">Dia</th>
                      <th scope="col" class="text-end">Acessos</th>
                      <th scope="col" class="text-end">Visitantes</th>
                      <th scope="col" class="text-end">Ocorrências</th>
                      <th scope="col" class="text-end">Avaliações</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="row in summary.byDay" :key="row.day">
                      <td>{{ formatDay(row.day) }}</td>
                      <td class="text-end">{{ row.visits }}</td>
                      <td class="text-end">{{ row.sessions }}</td>
                      <td class="text-end">{{ row.occurrences }}</td>
                      <td class="text-end">{{ row.feedback }}</td>
                    </tr>
                  </tbody>
                </v-table>
              </div>
            </v-card-text>
          </v-card>
        </v-col>

        <v-col cols="4" md="6">
          <v-card class="h-100">
            <v-card-item>
              <v-card-title class="text-title-large">Avaliações por perfil</v-card-title>
            </v-card-item>
            <v-card-text>
              <p v-if="!summary.feedbackByProfile.length" class="text-body-medium">Nenhuma avaliação ainda.</p>
              <v-table v-else>
                <thead>
                  <tr>
                    <th scope="col">Perfil</th>
                    <th scope="col" class="text-end">Avaliações</th>
                    <th scope="col" class="text-end">Nota média</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="row in summary.feedbackByProfile" :key="row.profile">
                    <td>{{ row.profile }}</td>
                    <td class="text-end">{{ row.total }}</td>
                    <td class="text-end">{{ row.averageRating }}</td>
                  </tr>
                </tbody>
              </v-table>
            </v-card-text>
          </v-card>
        </v-col>

        <v-col cols="4" md="6">
          <v-card class="h-100">
            <v-card-item>
              <v-card-title class="text-title-large">Últimos comentários</v-card-title>
            </v-card-item>
            <v-card-text>
              <p v-if="!summary.latestComments.length" class="text-body-medium">Nenhum comentário ainda.</p>
              <v-list v-else class="pa-0" lines="three">
                <v-list-item
                  v-for="(comment, index) in summary.latestComments"
                  :key="index"
                  :title="comment.comment"
                  :subtitle="`${comment.rating} ★ · ${comment.profile ?? 'Perfil não informado'} · ${formatDateTime(comment.createdAt)}`"
                  class="px-0"
                />
              </v-list>
            </v-card-text>
          </v-card>
        </v-col>

        <v-col cols="4" md="12">
          <v-card>
            <v-card-item>
              <v-card-title class="text-title-large">Planilhas para o relatório</v-card-title>
              <v-card-subtitle>Arquivos CSV que abrem no Excel ou no Google Planilhas</v-card-subtitle>
            </v-card-item>
            <v-card-text class="d-flex flex-wrap ga-2">
              <tonal-button
                v-for="spreadsheet in SPREADSHEETS"
                :key="spreadsheet.name"
                prepend-icon="mdi-download"
                :loading="downloading === spreadsheet.name"
                @click="download(spreadsheet)"
              >
                {{ spreadsheet.label }}
              </tonal-button>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>

      <v-card class="section">
        <v-card-item>
          <v-card-title class="text-title-large">Ocorrências</v-card-title>
          <v-card-subtitle>{{ occurrencesTotal }} no total, da mais recente para a mais antiga</v-card-subtitle>
        </v-card-item>
        <v-card-text>
          <p v-if="!occurrences.length && !loadingOccurrences" class="text-body-medium">Nenhuma ocorrência registrada.</p>
          <v-list v-else class="pa-0">
            <v-list-item
              v-for="occurrence in occurrences"
              :key="occurrence.id"
              :to="{ name: 'occurrence', params: { id: occurrence.id } }"
              :title="occurrence.neighborhood ?? 'Bairro não identificado'"
              :subtitle="cityStateAndDate(occurrence)"
              class="px-0"
            >
              <template #prepend>
                <WaterLevelChip :water-level="occurrence.waterLevel" size="small" class="mr-3 level-chip" />
              </template>
              <template #append>
                <v-btn
                  icon="mdi-delete-outline"
                  variant="text"
                  color="error"
                  :aria-label="`Excluir a ocorrência de ${occurrence.neighborhood ?? 'bairro não identificado'}`"
                  @click.prevent.stop="askToDelete(occurrence)"
                />
              </template>
            </v-list-item>
          </v-list>
          <v-btn
            v-if="occurrences.length < occurrencesTotal"
            variant="text"
            color="primary"
            class="mt-2"
            prepend-icon="mdi-chevron-down"
            :loading="loadingOccurrences"
            @click="loadOccurrences()"
          >
            Carregar mais
          </v-btn>
        </v-card-text>
      </v-card>

      <DeleteOccurrenceDialog v-model="deleteDialogOpen" :occurrence="occurrenceToDelete" @deleted="onDeleted" />
      <v-snackbar :model-value="!!deletedMessage" color="success" @update:model-value="deletedMessage = ''">
        {{ deletedMessage }}
      </v-snackbar>
    </template>
  </div>
</template>

<style scoped>
.chart {
  position: relative;
  height: 280px;
}

.table-scroll {
  max-height: 360px;
  overflow-y: auto;
}

.level-chip {
  flex-shrink: 0;
}
</style>
