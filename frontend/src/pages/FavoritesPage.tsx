import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { favoritesApi } from '@/services/api'
import { ServiceCard } from '@/components/common/ServiceCard'
import { ServiceCardSkeleton } from '@/components/common/LoadingSkeleton'

export function FavoritesPage() {
  const { data: favorites, isLoading } = useQuery({
    queryKey: ['favorites'],
    queryFn: favoritesApi.getAll,
  })

  return (
    <div className="min-h-screen p-4">
      <h1 className="text-xl font-semibold mb-4">Избранное</h1>

      {isLoading ? (
        <div className="grid grid-cols-2 gap-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <ServiceCardSkeleton key={i} />
          ))}
        </div>
      ) : favorites && favorites.length > 0 ? (
        <div className="grid grid-cols-2 gap-3">
          {favorites.map((service, index) => (
            <ServiceCard key={service.id} service={service} index={index} />
          ))}
        </div>
      ) : (
        <div className="text-center py-12">
          <div className="text-5xl mb-4">❤️</div>
          <h2 className="text-lg font-semibold mb-2">Список избранного пуст</h2>
          <p className="tg-hint mb-4">
            Нажмите на сердечко, чтобы добавить услугу в избранное
          </p>
          <Link to="/catalog" className="tg-button inline-block">
            Перейти в каталог
          </Link>
        </div>
      )}
    </div>
  )
}
