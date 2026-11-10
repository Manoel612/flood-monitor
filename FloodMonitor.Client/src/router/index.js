import { createRouter, createWebHistory } from 'vue-router'
import MapView from '@/views/MapView.vue'
import { recordPageVisit } from '@/services/api'

const routes = [
  { path: '/', name: 'map', component: MapView, meta: { title: 'Mapa' } },
  {
    path: '/occurrences',
    name: 'occurrences',
    component: () => import('@/views/OccurrencesView.vue'),
    meta: { title: 'Ocorrências registradas' }
  },
  {
    path: '/report',
    name: 'report',
    component: () => import('@/views/ReportView.vue'),
    meta: { title: 'Registrar enchente' }
  },
  {
    path: '/occurrences/:id(\\d+)',
    name: 'occurrence',
    component: () => import('@/views/OccurrenceView.vue'),
    props: true,
    meta: { title: 'Ocorrência' }
  },
  {
    path: '/metrics',
    name: 'metrics',
    component: () => import('@/views/MetricsView.vue'),
    meta: { title: 'Métricas' }
  },
  {
    path: '/rainfall',
    name: 'rainfall',
    component: () => import('@/views/RainfallView.vue'),
    meta: { title: 'Chuvas' }
  },
  {
    path: '/feedback',
    name: 'feedback',
    component: () => import('@/views/FeedbackView.vue'),
    meta: { title: 'Avaliar' }
  },
  {
    path: '/about',
    name: 'about',
    component: () => import('@/views/AboutView.vue'),
    meta: { title: 'Sobre' }
  },
  {
    path: '/admin',
    name: 'admin',
    component: () => import('@/views/AdminView.vue'),
    meta: { title: 'Área administrativa', skipVisitTracking: true }
  },
  { path: '/:pathMatch(.*)*', redirect: '/' }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.afterEach((to, _from, failure) => {
  if (failure) return
  document.title = to.meta.title ? `${to.meta.title} · Monitor de Enchentes` : 'Monitor de Enchentes'

  if (!to.meta.skipVisitTracking) recordPageVisit(String(to.name))
})

export default router
