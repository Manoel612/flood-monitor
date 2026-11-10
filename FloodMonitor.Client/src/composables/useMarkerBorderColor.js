import { computed } from 'vue'
import { useTheme } from 'vuetify'

export function useMarkerBorderColor() {
  const theme = useTheme()
  return computed(() => theme.current.value.colors['on-surface'])
}
