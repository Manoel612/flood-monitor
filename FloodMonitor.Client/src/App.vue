<script setup>
import { computed, ref, watchEffect } from 'vue'
import { useTheme } from 'vuetify'
import { THEME_STORAGE_KEY } from '@/plugins/vuetify'

const navigation = [
  { route: 'map', label: 'Mapa', icon: 'mdi-map-outline' },
  { route: 'occurrences', label: 'Ocorrências registradas', icon: 'mdi-format-list-bulleted' },
  { route: 'report', label: 'Registrar enchente', icon: 'mdi-plus-circle-outline' },
  { route: 'metrics', label: 'Métricas', icon: 'mdi-chart-box-outline' },
  { route: 'rainfall', label: 'Chuvas', icon: 'mdi-weather-pouring' },
  { route: 'feedback', label: 'Avaliar o aplicativo', icon: 'mdi-star-outline' }
]

const projectLinks = [
  { route: 'about', label: 'Sobre', icon: 'mdi-information-outline' },
  { route: 'admin', label: 'Área administrativa', icon: 'mdi-shield-lock-outline' }
]

const menuOpen = ref(false)

const theme = useTheme()
const isDark = computed(() => theme.current.value.dark)

function toggleTheme() {
  const next = isDark.value ? 'light' : 'dark'
  theme.change(next)
  try {
    localStorage.setItem(THEME_STORAGE_KEY, next)
  } catch {
  }
}

watchEffect(() => {
  document.querySelector('meta[name="theme-color"]')?.setAttribute('content', theme.current.value.colors.surface)
})
</script>

<template>
  <v-app>

    <v-navigation-drawer v-model="menuOpen" temporary width="304" aria-label="Menu principal">
      <div class="menu-header bg-primary pa-4">
        <v-btn
          icon="mdi-close"
          variant="text"
          class="close-menu"
          aria-label="Fechar menu"
          @click="menuOpen = false"
        />
        <v-icon icon="mdi-home-flood" size="40" />
        <div class="text-title-large mt-2">Monitor de Enchentes</div>
        <div class="text-body-medium">Mapa colaborativo de enchentes</div>
      </div>

      <v-list nav class="pa-3">
        <v-list-item
          v-for="item in navigation"
          :key="item.route"
          :to="{ name: item.route }"
          :prepend-icon="item.icon"
          :title="item.label"
          exact
          rounded="lg"
          active-class="bg-primary-container"
        />

        <v-divider class="my-3" />
        <v-list-subheader>Projeto</v-list-subheader>

        <v-list-item
          v-for="item in projectLinks"
          :key="item.route"
          :to="{ name: item.route }"
          :prepend-icon="item.icon"
          :title="item.label"
          exact
          rounded="lg"
          active-class="bg-primary-container"
        />
      </v-list>
    </v-navigation-drawer>

    <v-app-bar color="surface" border="b">
      <template #prepend>
        <v-app-bar-nav-icon aria-label="Abrir menu" @click="menuOpen = !menuOpen" />
      </template>
      <v-app-bar-title>
        <router-link :to="{ name: 'map' }" class="app-title">
          <v-icon icon="mdi-home-flood" color="primary" class="mr-2" />
          <span>Monitor de Enchentes</span>
        </router-link>
      </v-app-bar-title>
      <template #append>
        <v-btn
          :icon="isDark ? 'mdi-white-balance-sunny' : 'mdi-weather-night'"
          :aria-label="isDark ? 'Usar tema claro' : 'Usar tema escuro'"
          @click="toggleTheme"
        />
      </template>
    </v-app-bar>

    <v-main>
      <router-view />
    </v-main>
  </v-app>
</template>

<style scoped>
.app-title {
  color: inherit;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  font-weight: 500;
}

.menu-header {
  position: relative;
  min-height: 136px;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
}

.close-menu {
  position: absolute;
  top: 8px;
  right: 8px;
}
</style>
