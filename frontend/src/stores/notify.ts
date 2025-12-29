import { defineStore } from 'pinia'

export type NotifyLevel = 'success' | 'info' | 'warning' | 'error'

export const useNotifyStore = defineStore('notify', {
  state: () => ({
    show: false,
    message: '',
    level: 'info' as NotifyLevel,
    timeout: 4000,
  }),
  actions: {
    notify(message: string, level: NotifyLevel = 'info', timeout = 4000) {
      this.message = message
      this.level = level
      this.timeout = timeout
      this.show = true
    },
  },
})
