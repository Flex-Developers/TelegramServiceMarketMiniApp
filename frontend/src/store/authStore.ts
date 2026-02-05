import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { authApi } from '@/services/api'
import type { User } from '@/types'

interface AuthState {
  user: User | null
  accessToken: string | null
  refreshToken: string | null
  isLoading: boolean
  isAuthenticated: boolean
  authenticate: (initData: string) => Promise<void>
  refreshAuth: () => Promise<boolean>
  logout: () => void
  updateUser: (user: Partial<User>) => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isLoading: false,
      isAuthenticated: false,

      authenticate: async (initData: string) => {
        set({ isLoading: true })
        try {
          const result = await authApi.authenticateTelegram(initData)
          if (result.success && result.accessToken && result.user) {
            set({
              user: result.user,
              accessToken: result.accessToken,
              refreshToken: result.refreshToken ?? null,
              isAuthenticated: true,
              isLoading: false,
            })
          } else {
            set({ isLoading: false })
            console.error('Authentication failed:', result.error)
          }
        } catch (error) {
          set({ isLoading: false })
          console.error('Authentication error:', error)
        }
      },

      refreshAuth: async () => {
        const { refreshToken } = get()
        if (!refreshToken) return false

        try {
          const result = await authApi.refreshToken(refreshToken)
          if (result.success && result.accessToken) {
            set({
              accessToken: result.accessToken,
              refreshToken: result.refreshToken ?? null,
              user: result.user ?? get().user,
            })
            return true
          }
        } catch (error) {
          console.error('Token refresh failed:', error)
        }

        // On refresh failure, logout
        get().logout()
        return false
      },

      logout: () => {
        const { refreshToken } = get()
        if (refreshToken) {
          authApi.logout(refreshToken).catch(console.error)
        }
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          isAuthenticated: false,
        })
      },

      updateUser: (updates: Partial<User>) => {
        const { user } = get()
        if (user) {
          set({ user: { ...user, ...updates } })
        }
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
      }),
    }
  )
)
