import { MAP } from '@/config'
import { adminToken, endAdminSession, startAdminSession } from '@/services/adminSession'

const AUTHOR_ID_KEY = 'authorId'
const SESSION_ID_KEY = 'sessionId'

async function request(path, options = {}) {
  let response
  try {
    response = await fetch(path, options)
  } catch {
    throw new Error('Sem conexão com o servidor. Verifique sua internet e tente de novo.')
  }

  if (response.status === 429) {
    throw new Error('Muitos envios em pouco tempo. Aguarde alguns minutos e tente de novo.')
  }

  if (!response.ok) {
    let message = 'Não foi possível concluir a operação. Tente de novo.'
    try {
      const problem = await response.json()
      const errors = problem.errors ? Object.values(problem.errors).flat() : []
      if (errors.length) message = errors[0]
    } catch {
    }
    const error = new Error(message)
    error.status = response.status
    throw error
  }

  return response.status === 204 ? null : response.json()
}

function withQuery(path, filter = {}) {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(filter)) {
    if (value != null) params.set(key, value)
  }
  const query = params.toString()
  return query ? `${path}?${query}` : path
}

function postJson(path, data) {
  return request(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
  })
}

export const listOccurrences = (filter) => request(withQuery('/api/occurrences', filter))
export const getOccurrence = (id) => request(`/api/occurrences/${id}`)
export const getPlaces = () => request('/api/occurrences/places')

export const listRecentOccurrences = (filter, { onlyMine = false } = {}) =>
  request(
    withQuery('/api/occurrences/recent', onlyMine ? { ...filter, mine: true } : filter),
    { headers: authorHeader() }
  )

export const reportOccurrence = (formData) =>
  request('/api/occurrences', { method: 'POST', body: formData, headers: authorHeader() })

export const getMetricsSummary = (filter) => request(withQuery('/api/metrics/summary', filter))
export const getTotalsByNeighborhood = (filter) => request(withQuery('/api/metrics/by-neighborhood', filter))
export const getTotalsByMonth = (filter) => request(withQuery('/api/metrics/by-month', filter))
export const getHotspots = (filter) => request(withQuery('/api/metrics/hotspots', filter))

export const getRainfallCities = () => request('/api/rainfall/cities')
export const getCityRainfall = (city, state) => request(withQuery('/api/rainfall/monthly', { city, state }))

export async function searchAddress(text) {
  const [lat, lon] = MAP.center
  const params = new URLSearchParams({
    format: 'jsonv2',
    q: text,
    countrycodes: 'br',
    limit: '5',
    'accept-language': 'pt-BR',
    viewbox: [lon - 0.3, lat + 0.3, lon + 0.3, lat - 0.3].join(','),
    bounded: '0'
  })

  let response
  try {
    response = await fetch(`https://nominatim.openstreetmap.org/search?${params}`)
  } catch {
    throw new Error('Não foi possível buscar o endereço agora. Tente de novo ou filtre por cidade ou bairro.')
  }
  if (!response.ok) throw new Error('A busca de endereço não respondeu. Tente de novo ou filtre por cidade ou bairro.')

  const results = await response.json()
  const addresses = results.map((r) => ({
    label: r.display_name.split(', ').slice(0, 3).join(', '),
    lat: Number(r.lat),
    lon: Number(r.lon)
  }))

  return addresses.filter((a, i) => addresses.findIndex((other) => other.label === a.label) === i)
}

export const submitFeedback = (data) => postJson('/api/feedback', data)

export async function loginAsAdmin(username, password) {
  try {
    const { token } = await postJson('/api/admin/login', { username, password })
    startAdminSession(token)
  } catch (e) {
    if (e.status === 401) throw new Error('Usuário ou senha incorretos.')
    if (e.status === 404) throw new Error('A área administrativa não está configurada no servidor.')
    throw e
  }
}

async function adminRequest(path, options = {}) {
  try {
    return await request(path, {
      ...options,
      headers: { ...options.headers, Authorization: `Bearer ${adminToken.value}` }
    })
  } catch (e) {
    if (e.status === 401 || e.status === 404) {
      endAdminSession()
      throw new Error('Sua sessão de administrador terminou. Entre de novo.')
    }
    throw e
  }
}

export const getEvidenceSummary = () => adminRequest('/api/evidence/summary')

export const deleteOccurrence = (id) => adminRequest(`/api/occurrences/${id}`, { method: 'DELETE' })

export async function downloadEvidenceCsv(name, fileName) {
  let response
  try {
    response = await fetch(`/api/evidence/${name}.csv`, { headers: { Authorization: `Bearer ${adminToken.value}` } })
  } catch {
    throw new Error('Sem conexão com o servidor. Verifique sua internet e tente de novo.')
  }
  if (response.status === 401 || response.status === 404) {
    endAdminSession()
    throw new Error('Sua sessão de administrador terminou. Entre de novo.')
  }
  if (!response.ok) throw new Error('Não foi possível baixar a planilha. Tente de novo.')

  const url = URL.createObjectURL(await response.blob())
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.click()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

export function recordPageVisit(page) {
  fetch('/api/evidence/visits', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ page, sessionId: sessionId() }),
    keepalive: true
  }).catch(() => {})
}

let inMemorySessionId = null

function sessionId() {
  try {
    let id = sessionStorage.getItem(SESSION_ID_KEY)
    if (!id) {
      id = generateId()
      sessionStorage.setItem(SESSION_ID_KEY, id)
    }
    return id
  } catch {
    inMemorySessionId ??= generateId()
    return inMemorySessionId
  }
}

function deviceAuthorId() {
  try {
    let id = localStorage.getItem(AUTHOR_ID_KEY)
    if (!id) {
      id = generateId()
      localStorage.setItem(AUTHOR_ID_KEY, id)
    }
    return id
  } catch {
    return null
  }
}

function authorHeader() {
  const id = deviceAuthorId()
  return id ? { 'X-Author': id } : {}
}

function generateId() {
  return globalThis.crypto?.randomUUID?.() ??
    `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 12)}`
}
