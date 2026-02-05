import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { servicesApi, ordersApi } from '@/services/api'
import { useAuthStore } from '@/store/authStore'
import { PlusIcon, ChevronRightIcon } from '@/components/common/Icons'
import { ServiceCardSkeleton } from '@/components/common/LoadingSkeleton'

export function SellerDashboard() {
  const { user } = useAuthStore()

  const { data: services, isLoading: servicesLoading } = useQuery({
    queryKey: ['services', 'seller', user?.id],
    queryFn: () => servicesApi.getBySeller(user!.id),
    enabled: !!user,
  })

  const { data: orders } = useQuery({
    queryKey: ['orders', 'seller'],
    queryFn: () => ordersApi.getSellerOrders(1, 5),
  })

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('ru-RU', {
      style: 'currency',
      currency: 'RUB',
      maximumFractionDigits: 0,
    }).format(price)
  }

  // Calculate stats
  const totalServices = services?.length ?? 0
  const activeServices = services?.filter((s) => s.priceType).length ?? totalServices
  const pendingOrders = orders?.items.filter((o) => o.status === 'Paid' || o.status === 'Processing').length ?? 0

  return (
    <div className="min-h-screen pb-20">
      {/* Header */}
      <div className="bg-gradient-to-b from-tg-button/10 to-transparent p-4">
        <h1 className="text-xl font-bold mb-4">Кабинет продавца</h1>

        {/* Stats */}
        <div className="grid grid-cols-3 gap-3">
          <div className="tg-card text-center">
            <div className="text-2xl font-bold">{totalServices}</div>
            <div className="text-xs tg-hint">услуг</div>
          </div>
          <div className="tg-card text-center">
            <div className="text-2xl font-bold">{activeServices}</div>
            <div className="text-xs tg-hint">активных</div>
          </div>
          <div className="tg-card text-center">
            <div className="text-2xl font-bold text-tg-button">{pendingOrders}</div>
            <div className="text-xs tg-hint">в работе</div>
          </div>
        </div>
      </div>

      <div className="p-4 space-y-6">
        {/* Quick Actions */}
        <section>
          <Link
            to="/seller/services/new"
            className="tg-button w-full flex items-center justify-center gap-2"
          >
            <PlusIcon className="w-5 h-5" />
            Добавить услугу
          </Link>
        </section>

        {/* My Services */}
        <section>
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-semibold">Мои услуги</h2>
            {services && services.length > 4 && (
              <Link to="/seller/services" className="text-sm tg-link flex items-center gap-1">
                Все
                <ChevronRightIcon className="w-4 h-4" />
              </Link>
            )}
          </div>

          {servicesLoading ? (
            <div className="grid grid-cols-2 gap-3">
              {Array.from({ length: 2 }).map((_, i) => (
                <ServiceCardSkeleton key={i} />
              ))}
            </div>
          ) : services && services.length > 0 ? (
            <div className="space-y-3">
              {services.slice(0, 4).map((service, index) => (
                <motion.div
                  key={service.id}
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: index * 0.05 }}
                >
                  <Link
                    to={`/seller/services/${service.id}/edit`}
                    className="tg-card flex gap-3 active:scale-[0.98] transition-transform"
                  >
                    {service.thumbnailUrl ? (
                      <img
                        src={service.thumbnailUrl}
                        alt={service.title}
                        className="w-16 h-16 rounded-lg object-cover"
                      />
                    ) : (
                      <div className="w-16 h-16 rounded-lg bg-tg-secondary-bg flex items-center justify-center">
                        <span className="text-2xl">📷</span>
                      </div>
                    )}
                    <div className="flex-1 min-w-0">
                      <h3 className="font-medium text-sm line-clamp-1">{service.title}</h3>
                      <div className="text-sm text-tg-button font-medium">
                        {formatPrice(service.price)}
                      </div>
                      <div className="flex items-center gap-2 text-xs tg-hint mt-1">
                        <span>⭐ {service.averageRating.toFixed(1)}</span>
                        <span>•</span>
                        <span>{service.reviewCount} отзывов</span>
                      </div>
                    </div>
                    <ChevronRightIcon className="w-5 h-5 tg-hint self-center" />
                  </Link>
                </motion.div>
              ))}
            </div>
          ) : (
            <div className="tg-card text-center py-8">
              <div className="text-4xl mb-2">📦</div>
              <p className="tg-hint mb-4">У вас пока нет услуг</p>
              <Link to="/seller/services/new" className="tg-button inline-block">
                Создать первую услугу
              </Link>
            </div>
          )}
        </section>

        {/* Recent Orders */}
        <section>
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-semibold">Последние заказы</h2>
            <Link to="/orders" className="text-sm tg-link flex items-center gap-1">
              Все
              <ChevronRightIcon className="w-4 h-4" />
            </Link>
          </div>

          {orders && orders.items.length > 0 ? (
            <div className="space-y-2">
              {orders.items.slice(0, 3).map((order) => (
                <Link
                  key={order.id}
                  to={`/orders/${order.id}`}
                  className="tg-card flex items-center justify-between active:scale-[0.98] transition-transform"
                >
                  <div>
                    <div className="font-medium text-sm">{order.firstItemTitle}</div>
                    <div className="text-xs tg-hint">
                      {order.otherParty.firstName} •{' '}
                      {new Date(order.createdAt).toLocaleDateString('ru-RU')}
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="font-medium">{formatPrice(order.totalAmount)}</div>
                    <div className={`text-xs ${
                      order.status === 'Completed' ? 'text-green-600' :
                      order.status === 'Cancelled' ? 'text-red-600' :
                      'text-yellow-600'
                    }`}>
                      {order.status === 'Paid' ? 'Новый' :
                       order.status === 'Processing' ? 'В работе' :
                       order.status === 'Completed' ? 'Завершён' :
                       order.status === 'Cancelled' ? 'Отменён' :
                       order.status}
                    </div>
                  </div>
                </Link>
              ))}
            </div>
          ) : (
            <div className="tg-card text-center py-6">
              <p className="tg-hint">Заказов пока нет</p>
            </div>
          )}
        </section>
      </div>
    </div>
  )
}
