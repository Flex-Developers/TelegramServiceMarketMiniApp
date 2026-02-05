import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { Swiper, SwiperSlide } from 'swiper/react'
import { Pagination } from 'swiper/modules'
import 'swiper/css'
import 'swiper/css/pagination'
import { servicesApi, favoritesApi, reviewsApi } from '@/services/api'
import { useCartStore } from '@/store/cartStore'
import { useAuthStore } from '@/store/authStore'
import { useMainButton, useHapticFeedback } from '@/hooks/useTelegram'
import { HeartIcon, StarIcon, ShareIcon } from '@/components/common/Icons'
import { ServiceCardSkeleton } from '@/components/common/LoadingSkeleton'
import toast from 'react-hot-toast'
import WebApp from '@twa-dev/sdk'

export function ServicePage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { addItem } = useCartStore()
  const { isAuthenticated } = useAuthStore()
  const { impactOccurred, notificationOccurred } = useHapticFeedback()
  const [isAdding, setIsAdding] = useState(false)

  const { data, isLoading } = useQuery({
    queryKey: ['service', id],
    queryFn: () => servicesApi.getById(id!),
    enabled: !!id,
  })

  const { data: reviewStats } = useQuery({
    queryKey: ['reviews', 'stats', id],
    queryFn: () => reviewsApi.getStats(id!),
    enabled: !!id,
  })

  const { data: reviews } = useQuery({
    queryKey: ['reviews', 'service', id],
    queryFn: () => reviewsApi.getByService(id!, 1, 5),
    enabled: !!id,
  })

  const toggleFavorite = useMutation({
    mutationFn: async () => {
      if (data?.isFavorite) {
        await favoritesApi.remove(id!)
      } else {
        await favoritesApi.add(id!)
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['service', id] })
      impactOccurred('light')
    },
  })

  const handleAddToCart = async () => {
    if (!isAuthenticated) {
      toast.error('Войдите для добавления в корзину')
      return
    }

    setIsAdding(true)
    try {
      await addItem(id!)
      notificationOccurred('success')
      toast.success('Добавлено в корзину')
    } catch (error) {
      notificationOccurred('error')
      toast.error('Не удалось добавить в корзину')
    } finally {
      setIsAdding(false)
    }
  }

  const handleShare = () => {
    WebApp.openTelegramLink(
      `https://t.me/share/url?url=${encodeURIComponent(window.location.href)}&text=${encodeURIComponent(data?.service.title || '')}`
    )
  }

  useMainButton('Добавить в корзину', handleAddToCart, {
    isVisible: !!data?.service,
    isActive: !isAdding,
    isProgressVisible: isAdding,
  })

  if (isLoading) {
    return (
      <div className="p-4">
        <ServiceCardSkeleton />
      </div>
    )
  }

  if (!data?.service) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <p className="tg-hint">Услуга не найдена</p>
      </div>
    )
  }

  const { service, isFavorite } = data

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('ru-RU', {
      style: 'currency',
      currency: 'RUB',
      maximumFractionDigits: 0,
    }).format(price)
  }

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      className="pb-20"
    >
      {/* Image Gallery */}
      <div className="relative">
        <Swiper
          modules={[Pagination]}
          pagination={{ clickable: true }}
          className="aspect-square"
        >
          {service.images.length > 0 ? (
            service.images.map((image) => (
              <SwiperSlide key={image.id}>
                <img
                  src={image.imageUrl}
                  alt={service.title}
                  className="w-full h-full object-cover"
                />
              </SwiperSlide>
            ))
          ) : (
            <SwiperSlide>
              <div className="w-full h-full bg-tg-secondary-bg flex items-center justify-center">
                <span className="tg-hint text-6xl">📷</span>
              </div>
            </SwiperSlide>
          )}
        </Swiper>

        {/* Actions */}
        <div className="absolute top-4 right-4 flex gap-2 z-10">
          <button
            onClick={() => toggleFavorite.mutate()}
            className="w-10 h-10 rounded-full bg-white/90 backdrop-blur flex items-center justify-center shadow-lg"
          >
            <HeartIcon
              className={`w-5 h-5 ${isFavorite ? 'text-red-500' : 'text-gray-600'}`}
              filled={isFavorite}
            />
          </button>
          <button
            onClick={handleShare}
            className="w-10 h-10 rounded-full bg-white/90 backdrop-blur flex items-center justify-center shadow-lg"
          >
            <ShareIcon className="w-5 h-5 text-gray-600" />
          </button>
        </div>
      </div>

      {/* Content */}
      <div className="p-4 space-y-4">
        {/* Price & Title */}
        <div>
          <div className="text-2xl font-bold text-tg-button mb-2">
            {formatPrice(service.price)}
            {service.priceType === 'Hourly' && (
              <span className="text-base font-normal"> /час</span>
            )}
          </div>
          <h1 className="text-xl font-semibold">{service.title}</h1>
        </div>

        {/* Seller Info */}
        <div
          onClick={() => navigate(`/catalog?seller=${service.sellerId}`)}
          className="tg-card flex items-center gap-3 cursor-pointer active:scale-[0.98] transition-transform"
        >
          {service.seller.photoUrl ? (
            <img
              src={service.seller.photoUrl}
              alt={service.seller.firstName}
              className="w-12 h-12 rounded-full"
            />
          ) : (
            <div className="w-12 h-12 rounded-full bg-tg-button flex items-center justify-center text-white text-lg font-medium">
              {service.seller.firstName[0]}
            </div>
          )}
          <div className="flex-1">
            <div className="flex items-center gap-1">
              <span className="font-medium">{service.seller.firstName}</span>
              {service.seller.isVerified && (
                <span className="text-tg-button">✓</span>
              )}
            </div>
            <div className="flex items-center gap-2 text-sm tg-hint">
              <span className="flex items-center gap-1">
                <StarIcon className="w-4 h-4 text-yellow-500 fill-current" />
                {service.seller.averageRating.toFixed(1)}
              </span>
              <span>•</span>
              <span>Ответ за {service.responseTimeHours}ч</span>
            </div>
          </div>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-3 gap-3">
          <div className="tg-card text-center">
            <div className="text-lg font-bold">{service.deliveryDays}</div>
            <div className="text-xs tg-hint">дней</div>
          </div>
          <div className="tg-card text-center">
            <div className="text-lg font-bold">{service.orderCount}</div>
            <div className="text-xs tg-hint">заказов</div>
          </div>
          <div className="tg-card text-center">
            <div className="text-lg font-bold flex items-center justify-center gap-1">
              <StarIcon className="w-4 h-4 text-yellow-500 fill-current" />
              {service.averageRating.toFixed(1)}
            </div>
            <div className="text-xs tg-hint">{service.reviewCount} отзывов</div>
          </div>
        </div>

        {/* Description */}
        <div>
          <h2 className="font-semibold mb-2">Описание</h2>
          <p className="text-sm whitespace-pre-wrap">{service.description}</p>
        </div>

        {/* Tags */}
        {service.tags.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {service.tags.map((tag) => (
              <span
                key={tag}
                className="px-3 py-1 bg-tg-secondary-bg rounded-full text-sm"
              >
                {tag}
              </span>
            ))}
          </div>
        )}

        {/* Reviews */}
        {reviews && reviews.items.length > 0 && (
          <div>
            <h2 className="font-semibold mb-3">Отзывы</h2>

            {/* Rating Summary */}
            {reviewStats && (
              <div className="tg-card mb-3">
                <div className="flex items-center gap-4">
                  <div className="text-center">
                    <div className="text-3xl font-bold">{reviewStats.averageRating.toFixed(1)}</div>
                    <div className="flex">
                      {[1, 2, 3, 4, 5].map((star) => (
                        <StarIcon
                          key={star}
                          className={`w-4 h-4 ${
                            star <= Math.round(reviewStats.averageRating)
                              ? 'text-yellow-500 fill-current'
                              : 'text-gray-300'
                          }`}
                        />
                      ))}
                    </div>
                    <div className="text-xs tg-hint">{reviewStats.totalReviews} отзывов</div>
                  </div>
                  <div className="flex-1 space-y-1">
                    {[5, 4, 3, 2, 1].map((star) => {
                      const count = (reviewStats as any)[`${['one', 'two', 'three', 'four', 'five'][star - 1]}StarCount`]
                      const percentage = reviewStats.totalReviews > 0 ? (count / reviewStats.totalReviews) * 100 : 0
                      return (
                        <div key={star} className="flex items-center gap-2 text-xs">
                          <span className="w-3">{star}</span>
                          <div className="flex-1 h-2 bg-tg-secondary-bg rounded-full overflow-hidden">
                            <div
                              className="h-full bg-yellow-500 rounded-full"
                              style={{ width: `${percentage}%` }}
                            />
                          </div>
                        </div>
                      )
                    })}
                  </div>
                </div>
              </div>
            )}

            {/* Review List */}
            <div className="space-y-3">
              {reviews.items.map((review) => (
                <div key={review.id} className="tg-card">
                  <div className="flex items-center gap-2 mb-2">
                    {review.reviewer.photoUrl ? (
                      <img src={review.reviewer.photoUrl} alt="" className="w-8 h-8 rounded-full" />
                    ) : (
                      <div className="w-8 h-8 rounded-full bg-tg-button flex items-center justify-center text-white text-sm">
                        {review.reviewer.firstName[0]}
                      </div>
                    )}
                    <div className="flex-1">
                      <div className="font-medium text-sm">{review.reviewer.firstName}</div>
                      <div className="flex items-center gap-1">
                        {[1, 2, 3, 4, 5].map((star) => (
                          <StarIcon
                            key={star}
                            className={`w-3 h-3 ${
                              star <= review.rating ? 'text-yellow-500 fill-current' : 'text-gray-300'
                            }`}
                          />
                        ))}
                      </div>
                    </div>
                    <span className="text-xs tg-hint">
                      {new Date(review.createdAt).toLocaleDateString('ru-RU')}
                    </span>
                  </div>
                  {review.comment && (
                    <p className="text-sm">{review.comment}</p>
                  )}
                  {review.sellerResponse && (
                    <div className="mt-2 pl-3 border-l-2 border-tg-button">
                      <p className="text-xs tg-hint">Ответ продавца:</p>
                      <p className="text-sm">{review.sellerResponse}</p>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </motion.div>
  )
}
