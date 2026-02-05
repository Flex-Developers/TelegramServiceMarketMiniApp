import { useEffect, useRef, useCallback } from 'react'

export function useInfiniteScroll(
  onLoadMore: () => void,
  options: {
    hasNextPage: boolean
    isLoading: boolean
    rootMargin?: string
    threshold?: number
  }
) {
  const { hasNextPage, isLoading, rootMargin = '200px', threshold = 0.1 } = options
  const observerTarget = useRef<HTMLDivElement>(null)

  const handleObserver = useCallback(
    (entries: IntersectionObserverEntry[]) => {
      const [target] = entries
      if (target.isIntersecting && hasNextPage && !isLoading) {
        onLoadMore()
      }
    },
    [hasNextPage, isLoading, onLoadMore]
  )

  useEffect(() => {
    const element = observerTarget.current
    if (!element) return

    const observer = new IntersectionObserver(handleObserver, {
      rootMargin,
      threshold,
    })

    observer.observe(element)

    return () => {
      observer.unobserve(element)
      observer.disconnect()
    }
  }, [handleObserver, rootMargin, threshold])

  return { observerTarget }
}
