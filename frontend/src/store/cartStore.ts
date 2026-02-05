import { create } from 'zustand'
import { cartApi } from '@/services/api'
import type { Cart, CartItem } from '@/types'
import WebApp from '@twa-dev/sdk'

interface CartState {
  cart: Cart | null
  isLoading: boolean
  error: string | null
  fetchCart: () => Promise<void>
  addItem: (serviceId: string, quantity?: number) => Promise<void>
  updateQuantity: (itemId: string, quantity: number) => Promise<void>
  removeItem: (itemId: string) => Promise<void>
  clearCart: () => Promise<void>
  applyPromoCode: (code: string) => Promise<{ isValid: boolean; message?: string }>
}

export const useCartStore = create<CartState>((set, get) => ({
  cart: null,
  isLoading: false,
  error: null,

  fetchCart: async () => {
    set({ isLoading: true, error: null })
    try {
      const cart = await cartApi.getCart()
      set({ cart, isLoading: false })
    } catch (error) {
      set({ error: 'Не удалось загрузить корзину', isLoading: false })
    }
  },

  addItem: async (serviceId: string, quantity = 1) => {
    try {
      await cartApi.addItem(serviceId, quantity)
      // Haptic feedback
      WebApp.HapticFeedback.impactOccurred('light')
      // Refresh cart
      await get().fetchCart()
    } catch (error) {
      WebApp.HapticFeedback.notificationOccurred('error')
      throw error
    }
  },

  updateQuantity: async (itemId: string, quantity: number) => {
    // Optimistic update
    const { cart } = get()
    if (cart) {
      const updatedItems = cart.items.map(item =>
        item.id === itemId
          ? { ...item, quantity, totalPrice: item.servicePrice * quantity }
          : item
      )
      const newTotal = updatedItems.reduce((sum, item) => sum + item.totalPrice, 0)
      set({
        cart: {
          ...cart,
          items: updatedItems,
          subTotal: newTotal,
          total: newTotal - (cart.discountAmount ?? 0),
          itemCount: updatedItems.reduce((sum, item) => sum + item.quantity, 0),
        },
      })
    }

    try {
      await cartApi.updateItem(itemId, quantity)
      WebApp.HapticFeedback.selectionChanged()
    } catch (error) {
      // Revert on error
      await get().fetchCart()
      throw error
    }
  },

  removeItem: async (itemId: string) => {
    // Optimistic update
    const { cart } = get()
    if (cart) {
      const updatedItems = cart.items.filter(item => item.id !== itemId)
      const newTotal = updatedItems.reduce((sum, item) => sum + item.totalPrice, 0)
      set({
        cart: {
          ...cart,
          items: updatedItems,
          subTotal: newTotal,
          total: newTotal - (cart.discountAmount ?? 0),
          itemCount: updatedItems.reduce((sum, item) => sum + item.quantity, 0),
        },
      })
    }

    try {
      await cartApi.removeItem(itemId)
      WebApp.HapticFeedback.impactOccurred('medium')
    } catch (error) {
      // Revert on error
      await get().fetchCart()
      throw error
    }
  },

  clearCart: async () => {
    try {
      await cartApi.clearCart()
      set({ cart: { items: [], subTotal: 0, total: 0, itemCount: 0 } })
      WebApp.HapticFeedback.impactOccurred('heavy')
    } catch (error) {
      throw error
    }
  },

  applyPromoCode: async (code: string) => {
    try {
      const result = await cartApi.applyPromoCode(code)
      if (result.isValid) {
        await get().fetchCart()
        WebApp.HapticFeedback.notificationOccurred('success')
      }
      return result
    } catch (error) {
      WebApp.HapticFeedback.notificationOccurred('error')
      return { isValid: false, message: 'Ошибка применения промокода' }
    }
  },
}))
