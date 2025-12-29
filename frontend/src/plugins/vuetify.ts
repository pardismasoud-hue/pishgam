import '@mdi/font/css/materialdesignicons.css'
import 'vuetify/styles'
import { createVuetify } from 'vuetify'

const mspLightTheme = {
  dark: false,
  colors: {
    background: '#F6F2EA',
    surface: '#FFFFFF',
    primary: '#1E4E5D',
    secondary: '#D77A61',
    accent: '#3A8D7B',
    info: '#2E86AB',
    success: '#2E7D32',
    warning: '#E67E22',
    error: '#C0392B',
  },
}

export const vuetify = createVuetify({
  theme: {
    defaultTheme: 'mspLight',
    themes: {
      mspLight: mspLightTheme,
    },
  },
})
