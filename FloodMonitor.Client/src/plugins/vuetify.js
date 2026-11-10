import '@fontsource/roboto/400.css'
import '@fontsource/roboto/500.css'
import '@fontsource/roboto/700.css'
import '@mdi/font/css/materialdesignicons.css'
import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import { VBtn } from 'vuetify/components/VBtn'
import { pt } from 'vuetify/locale'
import { dark, fieldBorderOpacity, light, mediumEmphasisOpacity } from './palette'

export const THEME_STORAGE_KEY = 'theme'

function initialTheme() {
  try {
    const saved = localStorage.getItem(THEME_STORAGE_KEY)
    return saved === 'light' || saved === 'dark' ? saved : 'system'
  } catch {
    return 'system'
  }
}

export default createVuetify({
  locale: {
    locale: 'pt',
    messages: { pt }
  },

  theme: {
    defaultTheme: initialTheme(),
    themes: {
      light: {
        dark: false,
        colors: light,
        variables: {
          'medium-emphasis-opacity': mediumEmphasisOpacity.light,
          'field-border-opacity': fieldBorderOpacity.light
        }
      },
      dark: {
        dark: true,
        colors: dark,
        variables: {
          'medium-emphasis-opacity': mediumEmphasisOpacity.dark,
          'field-border-opacity': fieldBorderOpacity.dark
        }
      }
    }
  },

  aliases: {
    PrimaryButton: VBtn,
    TonalButton: VBtn
  },

  defaults: {
    PrimaryButton: { variant: 'elevated', color: 'primary', rounded: 'md', size: 'large' },
    TonalButton: { variant: 'flat', color: 'secondary-container', rounded: 'md', size: 'large' },
    VBtn: { rounded: 'md' },
    VBtnToggle: { rounded: 'md', density: 'comfortable' },

    VAppBar: { flat: true, density: 'comfortable' },
    VCard: { rounded: 'lg', elevation: 1 },
    VAlert: { variant: 'flat', rounded: 'lg', density: 'comfortable' },
    VChip: { rounded: 'md' },

    VTextField: { variant: 'outlined', rounded: 'md', density: 'comfortable', color: 'primary' },
    VTextarea: { variant: 'outlined', rounded: 'md', density: 'comfortable', color: 'primary' },
    VRadioGroup: { density: 'comfortable', color: 'primary' },
    VRating: { color: 'primary', activeColor: 'primary' },

    VRow: { density: 'comfortable' },
    VList: { density: 'comfortable' },
    VTable: { density: 'comfortable' },
    VProgressLinear: { color: 'primary', rounded: true },
    VProgressCircular: { color: 'primary' }
  }
})
