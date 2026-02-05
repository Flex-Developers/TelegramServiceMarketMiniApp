import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '@/store/authStore'
import type {
  AuthResult,
  Service,
  ServiceListItem,
  ServiceFilter,
  Category,
  Cart,
  CartItem,
  Order,
  OrderListItem,
  PaymentResult,
  Review,
  ReviewStats,
  Notification,
  PagedResult,
  PaymentMethod,
} from '@/types'

const api = axios.create({
  baseURL: '/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor for auth token
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = useAuthStore.getState().accessToken
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Response interceptor for token refresh
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true

      const refreshed = await useAuthStore.getState().refreshAuth()
      if (refreshed) {
        const token = useAuthStore.getState().accessToken
        originalRequest.headers.Authorization = `Bearer ${token}`
        return api(originalRequest)
      }
    }

    return Promise.reject(error)
  }
)

// Auth API
export const authApi = {
  authenticateTelegram: async (initData: string): Promise<AuthResult> => {
    const { data } = await api.post<AuthResult>('/auth/telegram', { initData })
    return data
  },

  refreshToken: async (refreshToken: string): Promise<AuthResult> => {
    const { data } = await api.post<AuthResult>('/auth/refresh', { refreshToken })
    return data
  },

  logout: async (refreshToken: string): Promise<void> => {
    await api.post('/auth/logout', { refreshToken })
  },
}

// Services API
export const servicesApi = {
  getServices: async (filter: ServiceFilter = {}): Promise<PagedResult<ServiceListItem>> => {
    const { data } = await api.get<PagedResult<ServiceListItem>>('/services', { params: filter })
    return data
  },

  getFeatured: async (count = 10): Promise<ServiceListItem[]> => {
    const { data } = await api.get<ServiceListItem[]>('/services/featured', { params: { count } })
    return data
  },

  getById: async (id: string): Promise<{ service: Service; isFavorite?: boolean }> => {
    const { data } = await api.get(`/services/${id}`)
    return data
  },

  getByCategory: async (categoryId: string, filter: ServiceFilter = {}): Promise<PagedResult<ServiceListItem>> => {
    const { data } = await api.get<PagedResult<ServiceListItem>>('/services', {
      params: { ...filter, categoryId },
    })
    return data
  },

  getBySeller: async (sellerId: string): Promise<ServiceListItem[]> => {
    const { data } = await api.get<ServiceListItem[]>(`/services/seller/${sellerId}`)
    return data
  },

  create: async (service: Partial<Service>): Promise<Service> => {
    const { data } = await api.post<Service>('/services', service)
    return data
  },

  update: async (id: string, service: Partial<Service>): Promise<Service> => {
    const { data } = await api.put<Service>(`/services/${id}`, service)
    return data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/services/${id}`)
  },

  activate: async (id: string): Promise<void> => {
    await api.post(`/services/${id}/activate`)
  },

  deactivate: async (id: string): Promise<void> => {
    await api.post(`/services/${id}/deactivate`)
  },
}

// Categories API
export const categoriesApi = {
  getAll: async (): Promise<Category[]> => {
    const { data } = await api.get<Category[]>('/categories')
    return data
  },

  getRoot: async (): Promise<Category[]> => {
    const { data } = await api.get<Category[]>('/categories/root')
    return data
  },

  getById: async (id: string): Promise<Category> => {
    const { data } = await api.get<Category>(`/categories/${id}`)
    return data
  },

  getChildren: async (parentId: string): Promise<Category[]> => {
    const { data } = await api.get<Category[]>(`/categories/${parentId}/children`)
    return data
  },

  // Admin methods
  create: async (category: {
    name: string
    nameEn?: string
    nameDe?: string
    icon?: string
    imageUrl?: string
    parentId?: string
    sortOrder?: number
  }): Promise<Category> => {
    const { data } = await api.post<Category>('/categories', category)
    return data
  },

  update: async (id: string, category: {
    name: string
    nameEn?: string
    nameDe?: string
    icon?: string
    imageUrl?: string
    parentId?: string
    sortOrder?: number
  }): Promise<void> => {
    await api.put(`/categories/${id}`, category)
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/categories/${id}`)
  },

  activate: async (id: string): Promise<void> => {
    await api.post(`/categories/${id}/activate`)
  },

  deactivate: async (id: string): Promise<void> => {
    await api.post(`/categories/${id}/deactivate`)
  },
}

// Cart API
export const cartApi = {
  getCart: async (): Promise<Cart> => {
    const { data } = await api.get<Cart>('/cart')
    return data
  },

  addItem: async (serviceId: string, quantity = 1): Promise<CartItem> => {
    const { data } = await api.post<CartItem>('/cart/items', { serviceId, quantity })
    return data
  },

  updateItem: async (itemId: string, quantity: number): Promise<CartItem> => {
    const { data } = await api.put<CartItem>(`/cart/items/${itemId}`, { quantity })
    return data
  },

  removeItem: async (itemId: string): Promise<void> => {
    await api.delete(`/cart/items/${itemId}`)
  },

  clearCart: async (): Promise<void> => {
    await api.delete('/cart/clear')
  },

  applyPromoCode: async (code: string): Promise<{ isValid: boolean; message?: string; discountAmount?: number; newTotal?: number }> => {
    const { data } = await api.post('/cart/promo', { code })
    return data
  },
}

// Orders API
export const ordersApi = {
  getMyOrders: async (page = 1, pageSize = 20): Promise<PagedResult<OrderListItem>> => {
    const { data } = await api.get<PagedResult<OrderListItem>>('/orders', { params: { page, pageSize } })
    return data
  },

  getSellerOrders: async (page = 1, pageSize = 20): Promise<PagedResult<OrderListItem>> => {
    const { data } = await api.get<PagedResult<OrderListItem>>('/orders/seller', { params: { page, pageSize } })
    return data
  },

  getById: async (id: string): Promise<Order> => {
    const { data } = await api.get<Order>(`/orders/${id}`)
    return data
  },

  create: async (paymentMethod: PaymentMethod, promoCode?: string, notes?: string): Promise<Order> => {
    const { data } = await api.post<Order>('/orders', { paymentMethod, promoCode, notes })
    return data
  },

  updateStatus: async (id: string, status: string): Promise<Order> => {
    const { data } = await api.put<Order>(`/orders/${id}/status`, { status })
    return data
  },

  cancel: async (id: string, reason: string): Promise<void> => {
    await api.post(`/orders/${id}/cancel`, { reason })
  },
}

// Payments API
export const paymentsApi = {
  createYooKassa: async (orderId: string, returnUrl?: string): Promise<PaymentResult> => {
    const { data } = await api.post<PaymentResult>('/payments/yookassa/create', { orderId, returnUrl })
    return data
  },

  createRobokassa: async (orderId: string, returnUrl?: string): Promise<PaymentResult> => {
    const { data } = await api.post<PaymentResult>('/payments/robokassa/create', { orderId, returnUrl })
    return data
  },

  createTelegramStars: async (orderId: string): Promise<PaymentResult> => {
    const { data } = await api.post<PaymentResult>('/payments/telegram/create', { orderId })
    return data
  },

  getStatus: async (paymentId: string): Promise<{ status: string }> => {
    const { data } = await api.get(`/payments/${paymentId}/status`)
    return data
  },
}

// Reviews API
export const reviewsApi = {
  getByService: async (serviceId: string, page = 1, pageSize = 10): Promise<PagedResult<Review>> => {
    const { data } = await api.get<PagedResult<Review>>(`/reviews/service/${serviceId}`, { params: { page, pageSize } })
    return data
  },

  getStats: async (serviceId: string): Promise<ReviewStats> => {
    const { data } = await api.get<ReviewStats>(`/reviews/service/${serviceId}/stats`)
    return data
  },

  create: async (orderId: string, rating: number, comment?: string, images?: string[]): Promise<Review> => {
    const { data } = await api.post<Review>('/reviews', { orderId, rating, comment, images })
    return data
  },

  addResponse: async (reviewId: string, response: string): Promise<Review> => {
    const { data } = await api.post<Review>(`/reviews/${reviewId}/response`, { response })
    return data
  },

  voteHelpful: async (reviewId: string): Promise<void> => {
    await api.post(`/reviews/${reviewId}/helpful`)
  },
}

// Favorites API
export const favoritesApi = {
  getAll: async (): Promise<ServiceListItem[]> => {
    const { data } = await api.get<ServiceListItem[]>('/favorites')
    return data
  },

  add: async (serviceId: string): Promise<void> => {
    await api.post(`/favorites/${serviceId}`)
  },

  remove: async (serviceId: string): Promise<void> => {
    await api.delete(`/favorites/${serviceId}`)
  },

  check: async (serviceId: string): Promise<boolean> => {
    const { data } = await api.get<{ isFavorite: boolean }>(`/favorites/${serviceId}/check`)
    return data.isFavorite
  },
}

// Users API
export const usersApi = {
  becomeSeller: async (): Promise<{ message: string; role: string }> => {
    const { data } = await api.post('/users/become-seller')
    return data
  },
}

// Notifications API
export const notificationsApi = {
  getAll: async (page = 1, pageSize = 20): Promise<PagedResult<Notification>> => {
    const { data } = await api.get<PagedResult<Notification>>('/notifications', { params: { page, pageSize } })
    return data
  },

  getSummary: async (): Promise<{ unreadCount: number; recent: Notification[] }> => {
    const { data } = await api.get('/notifications/summary')
    return data
  },

  markAsRead: async (id: string): Promise<void> => {
    await api.post(`/notifications/${id}/read`)
  },

  markAllAsRead: async (): Promise<void> => {
    await api.post('/notifications/read-all')
  },
}

export default api
