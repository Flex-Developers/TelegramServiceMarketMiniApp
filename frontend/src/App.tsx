import { useEffect } from 'react'
import { Routes, Route } from 'react-router-dom'
import WebApp from '@twa-dev/sdk'
import { useAuthStore } from './store/authStore'
import { Layout } from './components/common/Layout'
import { HomePage } from './pages/HomePage'
import { CatalogPage } from './pages/CatalogPage'
import { ServicePage } from './pages/ServicePage'
import { CartPage } from './pages/CartPage'
import { CheckoutPage } from './pages/CheckoutPage'
import { OrdersPage } from './pages/OrdersPage'
import { OrderDetailPage } from './pages/OrderDetailPage'
import { ProfilePage } from './pages/ProfilePage'
import { FavoritesPage } from './pages/FavoritesPage'
import { SellerDashboard } from './pages/SellerDashboard'
import { CreateServicePage } from './pages/CreateServicePage'
import { PaymentSuccessPage } from './pages/PaymentSuccessPage'
import { AdminCategoriesPage } from './pages/AdminCategoriesPage'
import { NotFoundPage } from './pages/NotFoundPage'

function App() {
  const { authenticate, isLoading } = useAuthStore()

  useEffect(() => {
    // Initialize Telegram WebApp
    WebApp.ready()
    WebApp.expand()

    // Set theme class based on Telegram color scheme
    const colorScheme = WebApp.colorScheme
    document.documentElement.classList.toggle('dark', colorScheme === 'dark')

    // Enable closing confirmation
    WebApp.enableClosingConfirmation()

    // Authenticate user
    if (WebApp.initData) {
      authenticate(WebApp.initData)
    }

    // Handle back button
    WebApp.BackButton.onClick(() => {
      window.history.back()
    })

    return () => {
      WebApp.BackButton.offClick(() => {})
    }
  }, [authenticate])

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="w-8 h-8 border-3 border-tg-button border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  return (
    <Layout>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/catalog" element={<CatalogPage />} />
        <Route path="/catalog/:categoryId" element={<CatalogPage />} />
        <Route path="/service/:id" element={<ServicePage />} />
        <Route path="/cart" element={<CartPage />} />
        <Route path="/checkout" element={<CheckoutPage />} />
        <Route path="/orders" element={<OrdersPage />} />
        <Route path="/orders/:id" element={<OrderDetailPage />} />
        <Route path="/profile" element={<ProfilePage />} />
        <Route path="/favorites" element={<FavoritesPage />} />
        <Route path="/seller" element={<SellerDashboard />} />
        <Route path="/seller/services/new" element={<CreateServicePage />} />
        <Route path="/seller/services/:id/edit" element={<CreateServicePage />} />
        <Route path="/payment/success" element={<PaymentSuccessPage />} />
        <Route path="/admin/categories" element={<AdminCategoriesPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </Layout>
  )
}

export default App
