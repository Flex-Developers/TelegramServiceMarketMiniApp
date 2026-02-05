import { useState, useMemo } from 'react'
import { useParams, useSearchParams } from 'react-router-dom'
import { useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { motion, AnimatePresence } from 'framer-motion'
import { servicesApi, categoriesApi } from '@/services/api'
import { ServiceCard } from '@/components/common/ServiceCard'
import { ServiceCardSkeleton } from '@/components/common/LoadingSkeleton'
import { SearchIcon, FilterIcon, CloseIcon } from '@/components/common/Icons'
import { useInfiniteScroll } from '@/hooks/useInfiniteScroll'
import { useHapticFeedback } from '@/hooks/useTelegram'
import type { ServiceFilter } from '@/types'
import { useDebounce } from '@/hooks/useDebounce'

export function CatalogPage() {
  const { categoryId } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const [searchTerm, setSearchTerm] = useState(searchParams.get('q') || '')
  const [showFilters, setShowFilters] = useState(false)
  const { selectionChanged } = useHapticFeedback()

  const debouncedSearch = useDebounce(searchTerm, 300)

  const [filters, setFilters] = useState<ServiceFilter>({
    categoryId,
    minPrice: undefined,
    maxPrice: undefined,
    minRating: undefined,
    sortBy: 'orders',
    sortDescending: true,
  })

  const { data: category } = useQuery({
    queryKey: ['category', categoryId],
    queryFn: () => categoriesApi.getById(categoryId!),
    enabled: !!categoryId,
  })

  const {
    data,
    fetchNextPage,
    hasNextPage,
    isLoading,
    isFetchingNextPage,
  } = useInfiniteQuery({
    queryKey: ['services', { ...filters, searchTerm: debouncedSearch }],
    queryFn: ({ pageParam = 1 }) =>
      servicesApi.getServices({
        ...filters,
        searchTerm: debouncedSearch || undefined,
        page: pageParam,
        pageSize: 20,
      }),
    getNextPageParam: (lastPage) =>
      lastPage.hasNextPage ? lastPage.page + 1 : undefined,
    initialPageParam: 1,
  })

  const services = useMemo(
    () => data?.pages.flatMap((page) => page.items) ?? [],
    [data]
  )

  const { observerTarget } = useInfiniteScroll(() => fetchNextPage(), {
    hasNextPage: !!hasNextPage,
    isLoading: isFetchingNextPage,
  })

  const handleSearch = (value: string) => {
    setSearchTerm(value)
    if (value) {
      setSearchParams({ q: value })
    } else {
      setSearchParams({})
    }
  }

  const handleFilterChange = (key: keyof ServiceFilter, value: any) => {
    selectionChanged()
    setFilters((prev) => ({ ...prev, [key]: value }))
  }

  const sortOptions = [
    { value: 'orders', label: 'По популярности' },
    { value: 'rating', label: 'По рейтингу' },
    { value: 'price', label: 'По цене' },
    { value: 'newest', label: 'По новизне' },
  ]

  return (
    <div className="min-h-screen">
      {/* Header */}
      <div className="sticky top-0 z-40 bg-tg-bg px-4 py-3 border-b border-tg-secondary-bg">
        <h1 className="text-lg font-semibold mb-3">
          {category?.name || 'Каталог услуг'}
        </h1>

        {/* Search */}
        <div className="flex gap-2">
          <div className="flex-1 relative">
            <SearchIcon className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 tg-hint" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => handleSearch(e.target.value)}
              placeholder="Поиск..."
              className="tg-input pl-10 pr-8"
            />
            {searchTerm && (
              <button
                onClick={() => handleSearch('')}
                className="absolute right-3 top-1/2 -translate-y-1/2 p-1"
              >
                <CloseIcon className="w-4 h-4 tg-hint" />
              </button>
            )}
          </div>
          <button
            onClick={() => setShowFilters(!showFilters)}
            className="tg-button-secondary p-3"
          >
            <FilterIcon className="w-5 h-5" />
          </button>
        </div>

        {/* Filters */}
        <AnimatePresence>
          {showFilters && (
            <motion.div
              initial={{ height: 0, opacity: 0 }}
              animate={{ height: 'auto', opacity: 1 }}
              exit={{ height: 0, opacity: 0 }}
              className="overflow-hidden"
            >
              <div className="pt-3 space-y-3">
                {/* Sort */}
                <div>
                  <label className="text-sm tg-hint mb-1 block">Сортировка</label>
                  <select
                    value={filters.sortBy}
                    onChange={(e) => handleFilterChange('sortBy', e.target.value)}
                    className="tg-input"
                  >
                    {sortOptions.map((opt) => (
                      <option key={opt.value} value={opt.value}>
                        {opt.label}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Price Range */}
                <div className="flex gap-2">
                  <div className="flex-1">
                    <label className="text-sm tg-hint mb-1 block">Цена от</label>
                    <input
                      type="number"
                      value={filters.minPrice || ''}
                      onChange={(e) =>
                        handleFilterChange('minPrice', e.target.value ? Number(e.target.value) : undefined)
                      }
                      placeholder="0"
                      className="tg-input"
                    />
                  </div>
                  <div className="flex-1">
                    <label className="text-sm tg-hint mb-1 block">Цена до</label>
                    <input
                      type="number"
                      value={filters.maxPrice || ''}
                      onChange={(e) =>
                        handleFilterChange('maxPrice', e.target.value ? Number(e.target.value) : undefined)
                      }
                      placeholder="∞"
                      className="tg-input"
                    />
                  </div>
                </div>

                {/* Rating */}
                <div>
                  <label className="text-sm tg-hint mb-1 block">Минимальный рейтинг</label>
                  <div className="flex gap-2">
                    {[0, 3, 4, 4.5].map((rating) => (
                      <button
                        key={rating}
                        onClick={() => handleFilterChange('minRating', rating || undefined)}
                        className={`flex-1 py-2 rounded-lg text-sm transition-colors ${
                          filters.minRating === rating || (!filters.minRating && rating === 0)
                            ? 'bg-tg-button text-tg-button-text'
                            : 'bg-tg-secondary-bg'
                        }`}
                      >
                        {rating === 0 ? 'Все' : `${rating}+`}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      {/* Services Grid */}
      <div className="p-4">
        {isLoading ? (
          <div className="grid grid-cols-2 gap-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <ServiceCardSkeleton key={i} />
            ))}
          </div>
        ) : services.length === 0 ? (
          <div className="text-center py-12">
            <p className="tg-hint text-lg mb-2">Услуги не найдены</p>
            <p className="text-sm tg-hint">
              Попробуйте изменить параметры поиска
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-2 gap-3">
            {services.map((service, index) => (
              <ServiceCard key={service.id} service={service} index={index} />
            ))}
          </div>
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
