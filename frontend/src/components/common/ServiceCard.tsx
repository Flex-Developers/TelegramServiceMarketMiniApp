import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import type { ServiceListItem } from '@/types'
import { StarIcon } from './Icons'

interface ServiceCardProps {
  service: ServiceListItem
  index?: number
}

export function ServiceCard({ service, index = 0 }: ServiceCardProps) {
  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('ru-RU', {
      style: 'currency',
      currency: 'RUB',
      maximumFractionDigits: 0,
    }).format(price)
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3, delay: index * 0.05 }}
    >
      <Link
        to={`/service/${service.id}`}
        className="block tg-card overflow-hidden hover:shadow-lg transition-shadow"
      >
        {/* Image */}
        <div className="aspect-[4/3] bg-tg-secondary-bg relative overflow-hidden rounded-lg mb-3">
          {service.thumbnailUrl ? (
            <img
              src={service.thumbnailUrl}
              alt={service.title}
              className="w-full h-full object-cover"
              loading="lazy"
            />
          ) : (
            <div className="w-full h-full flex items-center justify-center text-tg-hint">
              <svg className="w-12 h-12" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
            </div>
          )}
        </div>

        {/* Content */}
        <div>
          {/* Seller */}
          <div className="flex items-center gap-2 mb-2">
            {service.seller.photoUrl ? (
              <img
                src={service.seller.photoUrl}
                alt={service.seller.firstName}
                className="w-5 h-5 rounded-full"
              />
            ) : (
              <div className="w-5 h-5 rounded-full bg-tg-button flex items-center justify-center text-white text-xs">
                {service.seller.firstName[0]}
              </div>
            )}
            <span className="text-xs tg-hint truncate">
              {service.seller.firstName}
              {service.seller.isVerified && (
                <span className="ml-1 text-tg-button">✓</span>
              )}
            </span>
          </div>

          {/* Title */}
          <h3 className="font-medium text-sm line-clamp-2 mb-2">{service.title}</h3>

          {/* Rating & Reviews */}
          <div className="flex items-center gap-1 mb-2">
            <StarIcon className="w-4 h-4 text-yellow-500 fill-current" />
            <span className="text-sm font-medium">
              {service.averageRating > 0 ? service.averageRating.toFixed(1) : 'Нет'}
            </span>
            <span className="text-xs tg-hint">
              ({service.reviewCount})
            </span>
          </div>

          {/* Price & Delivery */}
          <div className="flex items-center justify-between">
            <span className="font-bold text-tg-button">
              {formatPrice(service.price)}
              {service.priceType === 'Hourly' && <span className="text-xs font-normal">/час</span>}
            </span>
            <span className="text-xs tg-hint">
              {service.deliveryDays} {service.deliveryDays === 1 ? 'день' : 'дней'}
            </span>
          </div>
        </div>
      </Link>
    </motion.div>
  )
}
