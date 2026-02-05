import { useEffect, useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { useCartStore } from '@/store/cartStore'
import { useMainButton, useHapticFeedback } from '@/hooks/useTelegram'
import { PlusIcon, MinusIcon, TrashIcon } from '@/components/common/Icons'
import { CartItemSkeleton } from '@/components/common/LoadingSkeleton'
import toast from 'react-hot-toast'

export function CartPage() {
  const navigate = useNavigate()
  const { cart, isLoading, fetchCart, updateQuantity, removeItem, applyPromoCode } = useCartStore()
  const { impactOccurred } = useHapticFeedback()
  const [promoCode, setPromoCode] = useState('')
  const [isApplyingPromo, setIsApplyingPromo] = useState(false)

  useEffect(() => {
    fetchCart()
  }, [fetchCart])

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('ru-RU', {
      style: 'currency',
      currency: 'RUB',
      maximumFractionDigits: 0,
    }).format(price)
  }

  const handleCheckout = () => {
    if (cart && cart.items.length > 0) {
      navigate('/checkout')
    }
  }

  useMainButton(
    cart && cart.items.length > 0
      ? `Оформить заказ • ${formatPrice(cart.total)}`
      : 'Корзина пуста',
    handleCheckout,
    {
      isVisible: true,
      isActive: cart ? cart.items.length > 0 : false,
    }
  )

  const handleQuantityChange = async (itemId: string, newQuantity: number) => {
    impactOccurred('light')
    try {
      await updateQuantity(itemId, newQuantity)
    } catch (error) {
      toast.error('Не удалось обновить количество')
    }
  }

  const handleRemove = async (itemId: string) => {
    impactOccurred('medium')
    try {
      await removeItem(itemId)
      toast.success('Удалено из корзины')
    } catch (error) {
      toast.error('Не удалось удалить')
    }
  }

  const handleApplyPromo = async () => {
    if (!promoCode.trim()) return

    setIsApplyingPromo(true)
    try {
      const result = await applyPromoCode(promoCode.trim())
      if (result.isValid) {
        toast.success(result.message || 'Промокод применён')
        setPromoCode('')
      } else {
        toast.error(result.message || 'Недействительный промокод')
      }
    } finally {
      setIsApplyingPromo(false)
    }
  }

  if (isLoading) {
    return (
      <div className="p-4 space-y-3">
        <h1 className="text-xl font-semibold mb-4">Корзина</h1>
        {Array.from({ length: 3 }).map((_, i) => (
          <CartItemSkeleton key={i} />
        ))}
      </div>
    )
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] p-4">
        <div className="text-6xl mb-4">🛒</div>
        <h2 className="text-xl font-semibold mb-2">Корзина пуста</h2>
        <p className="tg-hint text-center mb-6">
          Добавьте услуги из каталога, чтобы оформить заказ
        </p>
        <Link to="/catalog" className="tg-button">
          Перейти в каталог
        </Link>
      </div>
    )
  }

  return (
    <div className="p-4 pb-24">
      <h1 className="text-xl font-semibold mb-4">
        Корзина ({cart.itemCount})
      </h1>

      {/* Cart Items */}
      <div className="space-y-3 mb-4">
        <AnimatePresence>
          {cart.items.map((item) => (
            <motion.div
              key={item.id}
              layout
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: 20, height: 0 }}
              className="tg-card flex gap-3"
            >
              {/* Image */}
              <Link to={`/service/${item.serviceId}`} className="flex-shrink-0">
                {item.thumbnailUrl ? (
                  <img
                    src={item.thumbnailUrl}
                    alt={item.serviceTitle}
                    className="w-20 h-20 rounded-lg object-cover"
                  />
                ) : (
                  <div className="w-20 h-20 rounded-lg bg-tg-secondary-bg flex items-center justify-center">
                    <span className="text-2xl">📷</span>
                  </div>
                )}
              </Link>

              {/* Content */}
              <div className="flex-1 min-w-0">
                <Link to={`/service/${item.serviceId}`}>
                  <h3 className="font-medium text-sm line-clamp-2 mb-1">
                    {item.serviceTitle}
                  </h3>
                </Link>
                <div className="text-xs tg-hint mb-2">
                  {item.seller.firstName}
                </div>

                <div className="flex items-center justify-between">
                  <span className="font-bold text-tg-button">
                    {formatPrice(item.totalPrice)}
                  </span>

                  <div className="flex items-center gap-2">
                    <button
                      onClick={() =>
                        item.quantity > 1
                          ? handleQuantityChange(item.id, item.quantity - 1)
                          : handleRemove(item.id)
                      }
                      className="w-8 h-8 rounded-lg bg-tg-secondary-bg flex items-center justify-center active:scale-95 transition-transform"
                    >
                      {item.quantity === 1 ? (
                        <TrashIcon className="w-4 h-4 tg-destructive" />
                      ) : (
                        <MinusIcon className="w-4 h-4" />
                      )}
                    </button>

                    <span className="w-8 text-center font-medium">
                      {item.quantity}
                    </span>

                    <button
                      onClick={() => handleQuantityChange(item.id, item.quantity + 1)}
                      className="w-8 h-8 rounded-lg bg-tg-secondary-bg flex items-center justify-center active:scale-95 transition-transform"
                    >
                      <PlusIcon className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              </div>
            </motion.div>
          ))}
        </AnimatePresence>
      </div>

      {/* Promo Code */}
      <div className="flex gap-2 mb-4">
        <input
          type="text"
          value={promoCode}
          onChange={(e) => setPromoCode(e.target.value.toUpperCase())}
          placeholder="Промокод"
          className="tg-input flex-1"
          maxLength={20}
        />
        <button
          onClick={handleApplyPromo}
          disabled={!promoCode.trim() || isApplyingPromo}
          className="tg-button disabled:opacity-50"
        >
          {isApplyingPromo ? '...' : 'Применить'}
        </button>
      </div>

      {/* Summary */}
      <div className="tg-card space-y-2">
        <div className="flex justify-between">
          <span className="tg-hint">Подытог</span>
          <span>{formatPrice(cart.subTotal)}</span>
        </div>

        {cart.discountAmount && cart.discountAmount > 0 && (
          <div className="flex justify-between text-green-600">
            <span>Скидка ({cart.promoCode})</span>
            <span>-{formatPrice(cart.discountAmount)}</span>
          </div>
        )}

        <div className="border-t border-tg-secondary-bg pt-2 flex justify-between font-bold text-lg">
          <span>Итого</span>
          <span className="text-tg-button">{formatPrice(cart.total)}</span>
        </div>
      </div>
    </div>
  )
}
