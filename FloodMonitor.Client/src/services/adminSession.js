import { computed, ref } from 'vue'

const ADMIN_TOKEN_KEY = 'adminToken'

function readSavedToken() {
  try {
    return sessionStorage.getItem(ADMIN_TOKEN_KEY)
  } catch {
    return null
  }
}

export const adminToken = ref(readSavedToken())

export const isAdmin = computed(() => !!adminToken.value)

export function startAdminSession(token) {
  adminToken.value = token
  try {
    sessionStorage.setItem(ADMIN_TOKEN_KEY, token)
  } catch {
  }
}

export function endAdminSession() {
  adminToken.value = null
  try {
    sessionStorage.removeItem(ADMIN_TOKEN_KEY)
  } catch {
  }
}
