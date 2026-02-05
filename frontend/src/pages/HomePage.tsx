import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { categoriesApi, servicesApi } from '@/services/api'
import { ServiceCard } from '@/components/common/ServiceCard'
import { ServiceCardSkeleton, CategoryCardSkeleton } from '@/components/common/LoadingSkeleton'
import { SearchIcon, ChevronRightIcon } from '@/components/common/Icons'

const iconMap: Record<string, string> = {
  palette: '🎨',
  code: '💻',
  megaphone: '📢',
  pencil: '✏️',
  design: '🎨',
  development: '💻',
  marketing: '📢',
  copywriting: '✏️',
  photo: '📷',
  video: '🎬',
  music: '🎵',
  translation: '🌐',
  education: '📚',
  consulting: '💼',
  default: '📦',
}

function getCategoryIcon(icon: string | undefined): string {
  if (!icon) return iconMap.default
  return iconMap[icon.toLowerCase()] || icon
}

export function HomePage() {
  const { data: categories, isLoading: categoriesLoading } = useQuery({
    queryKey: ['categories', 'root'],
    queryFn: categoriesApi.getRoot,
  })

  const { data: featuredServices, isLoading: servicesLoading } = useQuery({
    queryKey: ['services', 'featured'],
    queryFn: () => servicesApi.getFeatured(8),
  })

  return (
    <div className="min-h-screen">
      {/* Header */}
      <div className="px-4 py-6 bg-gradient-to-b from-tg-button/10 to-transparent">
        <h1 className="text-2xl font-bold mb-4">Маркетплейс услуг</h1>

        {/* Search */}
        <Link to="/catalog" className="block">
          <div className="tg-input flex items-center gap-3">
            <SearchIcon className="w-5 h-5 tg-hint" />
            <span className="tg-hint">Поиск услуг...</span>
          </div>
        </Link>
      </div>

      {/* Categories */}
      <section className="px-4 mb-6">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-lg font-semibold">Категории</h2>
          <Link to="/catalog" className="text-sm tg-link flex items-center gap-1">
            Все
            <ChevronRightIcon className="w-4 h-4" />
          </Link>
        </div>

        <div className="grid grid-cols-2 gap-3">
          {categoriesLoading
            ? Array.from({ length: 6 }).map((_, i) => <CategoryCardSkeleton key={i} />)
            : categories?.slice(0, 6).map((category, index) => (
                <motion.div
                  key={category.id}
                  initial={{ opacity: 0, scale: 0.95 }}
                  animate={{ opacity: 1, scale: 1 }}
                  transition={{ duration: 0.2, delay: index * 0.05 }}
                >
                  <Link
                    to={`/catalog/${category.id}`}
                    className="tg-card flex items-center gap-3 active:scale-[0.98] transition-transform"
                  >
                    <div className="w-12 h-12 rounded-xl bg-tg-button/10 flex items-center justify-center text-2xl">
                      {getCategoryIcon(category.icon)}
                    </div>
                    <div className="flex-1 min-w-0">
                      <h3 className="font-medium text-sm truncate">{category.name}</h3>
                      <p className="text-xs tg-hint">{category.serviceCount} услуг</p>
                    </div>
                  </Link>
                </motion.div>
              ))}
        </div>
      </section>

      {/* Featured Services */}
      <section className="px-4 pb-6">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-lg font-semibold">Популярные услуги</h2>
          <Link to="/catalog" className="text-sm tg-link flex items-center gap-1">
            Все
            <ChevronRightIcon className="w-4 h-4" />
          </Link>
        </div>

        <div className="grid grid-cols-2 gap-3">
          {servicesLoading
            ? Array.from({ length: 4 }).map((_, i) => <ServiceCardSkeleton key={i} />)
            : featuredServices?.map((service, index) => (
                <ServiceCard key={service.id} service={service} index={index} />
              ))}
        </div>
      </section>
    </div>
  )
}
