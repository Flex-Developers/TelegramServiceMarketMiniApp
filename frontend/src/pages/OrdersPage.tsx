import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useInfiniteQuery } from '@tanstack/react-query'
import { motion, AnimatePresence } from 'framer-motion'
import { ordersApi } from '@/services/api'
import { useAuthStore } from '@/store/authStore'
import { useInfiniteScroll } from '@/hooks/useInfiniteScroll'
import { OrderCardSkeleton } from '@/components/common/LoadingSkeleton'
import { ChevronRightIcon } from '@/components/common/Icons'
import type { OrderStatus } from '@/types'

const statusLabels: Record<OrderStatus, { label: string; color: string }> = {
  Pending: { label: 'Ожидает оплаты', color: 'bg-yellow-100 text-yellow-800' },
  Paid: { label: 'Оплачен', color: 'bg-blue-100 text-blue-800' },
  Processing: { label: 'В работе', color: 'bg-purple-100 text-purple-800' },
  Delivered: { label: 'Доставлен', color: 'bg-green-100 text-green-800' },
  Completed: { label: 'Завершён', color: 'bg-green-100 text-green-800' },
  Cancelled: { label: 'Отменён', color: 'bg-red-100 text-red-800' },
  Refunded: { label: 'Возврат', color: 'bg-gray-100 text-gray-800' },
  Disputed: { label: 'Спор', color: 'bg-orange-100 text-orange-800' },
}

export function OrdersPage() {
  const { user } = useAuthStore()
  const [view, setView] = useState<'buyer' | 'seller'>('buyer')

  const {
    data,
    fetchNextPage,
    hasNextPage,
    isLoading,
    isFetchingNextPage,
  } = useInfiniteQuery({
    queryKey: ['orders', view],
    queryFn: ({ pageParam }) =>
      view === 'buyer'
        ? ordersApi.getMyOrders(pageParam, 20)
        : ordersApi.getSellerOrders(pageParam, 20),
    getNextPageParam: (lastPage) =>
      lastPage?.hasNextPage ? lastPage.page + 1 : undefined,
    initialPageParam: 1,
  })

  const orders = data?.pages.flatMap((page) => page.items) ?? []

  const { observerTarget } = useInfiniteScroll(() => fetchNextPage(), {
    hasNextPage: !!hasNextPage,
    isLoading: isFetchingNextPage,
  })

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('ru-RU', {
      style: 'currency',
      currency: 'RUB',
      maximumFractionDigits: 0,
    }).format(price)
  }

  const formatDate = (date: string) => {
    return new Date(date).toLocaleDateString('ru-RU', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    })
  }

  const roleStr = String(user?.role ?? '')
  const isSeller = roleStr === 'Seller' || roleStr === '1' || roleStr === 'Both' || roleStr === '2' || roleStr === 'Admin' || roleStr === '3'

  return (
    <div className="min-h-screen">
      {/* Header */}
      <div className="sticky top-0 z-40 bg-tg-bg px-4 py-3 border-b border-tg-secondary-bg">
        <h1 className="text-xl font-semibold mb-3">Заказы</h1>

        {/* Tabs (for sellers) */}
        {isSeller && (
          <div className="flex gap-2">
            <button
              onClick={() => setView('buyer')}
              className={`flex-1 py-2 rounded-lg text-sm font-medium transition-colors ${
                view === 'buyer'
                  ? 'bg-tg-button text-tg-button-text'
                  : 'bg-tg-secondary-bg'
              }`}
            >
              Мои покупки
            </button>
            <button
              onClick={() => setView('seller')}
              className={`flex-1 py-2 rounded-lg text-sm font-medium transition-colors ${
                view === 'seller'
                  ? 'bg-tg-button text-tg-button-text'
                  : 'bg-tg-secondary-bg'
              }`}
            >
              Мои продажи
            </button>
          </div>
        )}
      </div>

      {/* Orders List */}
      <div className="p-4">
        {isLoading ? (
          <div className="space-y-3">
            {Array.from({ length: 5 }).map((_, i) => (
              <OrderCardSkeleton key={i} />
            ))}
          </div>
        ) : orders.length === 0 ? (
          <div className="text-center py-12">
            <div className="text-5xl mb-4">📦</div>
            <h2 className="text-lg font-semibold mb-2">Заказов пока нет</h2>
            <p className="tg-hint mb-4">
              {view === 'buyer'
                ? 'Ваши покупки появятся здесь'
                : 'Ваши продажи появятся здесь'}
            </p>
            <Link to="/catalog" className="tg-button inline-block">
              Перейти в каталог
            </Link>
          </div>
        ) : (
          <AnimatePresence>
            <div className="space-y-3">
              {orders.map((order, index) => (
                <motion.div
                  key={order.id}
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.2, delay: index * 0.05 }}
                >
                  <Link
                    to={`/orders/${order.id}`}
                    className="tg-card flex items-start gap-3 active:scale-[0.98] transition-transform"
                  >
                    {/* Image */}
                    {order.firstItemThumbnail ? (
                      <img
                        src={order.firstItemThumbnail}
                        alt=""
                        className="w-16 h-16 rounded-lg object-cover flex-shrink-0"
                      />
                    ) : (
                      <div className="w-16 h-16 rounded-lg bg-tg-secondary-bg flex items-center justify-center flex-shrink-0">
                        <span className="text-2xl">📦</span>
                      </div>
                    )}

                    {/* Content */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-start justify-between gap-2">
                        <h3 className="font-medium text-sm line-clamp-1">
                          {order.firstItemTitle}
                          {order.itemCount > 1 && (
                            <span className="tg-hint"> +{order.itemCount - 1}</span>
                          )}
                        </h3>
                        <ChevronRightIcon className="w-5 h-5 tg-hint flex-shrink-0" />
                      </div>

                      <div className="flex items-center gap-2 mt-1">
                        {order.otherParty.photoUrl ? (
                          <img
                            src={order.otherParty.photoUrl}
                            alt=""
                            className="w-4 h-4 rounded-full"
                          />
                        ) : (
                          <div className="w-4 h-4 rounded-full bg-tg-button flex items-center justify-center text-white text-[10px]">
                            {order.otherParty.firstName[0]}
                          </div>
                        )}
                        <span className="text-xs tg-hint">
                          {order.otherParty.firstName}
                        </span>
                      </div>

                      <div className="flex items-center justify-between mt-2">
                        <span
                          className={`text-xs px-2 py-0.5 rounded-full ${
                            statusLabels[order.status].color
                          }`}
                        >
                          {statusLabels[order.status].label}
                        </span>
                        <span className="font-medium text-sm">
                          {formatPrice(order.totalAmount)}
                        </span>
                      </div>

                      <div className="text-xs tg-hint mt-1">
                        {formatDate(order.createdAt)}
                      </div>
                    </div>
                  </Link>
                </motion.div>
              ))}
            </div>
          </AnimatePresence>
        )}

        {/* Load More Trigger */}
        <div ref={observerTarget} className="h-10" />

        {isFetchingNextPage && (
          <div className="flex justify-center py-4">
            <div className="w-6 h-6 border-2 border-tg-button border-t-transparent rounded-full animate-spin" />
          </div>
        )}
      </div>
    </div>
  )
}
