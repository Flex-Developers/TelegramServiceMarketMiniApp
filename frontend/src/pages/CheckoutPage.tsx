import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { ordersApi, paymentsApi } from '@/services/api'
import { useCartStore } from '@/store/cartStore'
import { useMainButton, useHapticFeedback, usePopup } from '@/hooks/useTelegram'
import type { PaymentMethod } from '@/types'
import toast from 'react-hot-toast'
import WebApp from '@twa-dev/sdk'

const paymentMethods: { id: PaymentMethod; name: string; icon: string; description: string }[] = [
  {
    id: 'YooKassa',
    name: 'ЮKassa',
    icon: '💳',
    description: 'Банковская карта, SberPay, ЮMoney',
  },
  {
    id: 'Robokassa',
    name: 'Robokassa',
    icon: '🏦',
    description: 'Карта, электронные кошельки',
  },
  {
    id: 'TelegramStars',
    name: 'Telegram Stars',
    icon: '⭐',
    description: 'Оплата через Telegram',
  },
]

export function CheckoutPage() {
  const navigate = useNavigate()
  const { cart, clearCart } = useCartStore()
  const { notificationOccurred } = useHapticFeedback()
  const { showConfirm } = usePopup()

  const [selectedPayment, setSelectedPayment] = useState<PaymentMethod>('YooKassa')
  const [notes, setNotes] = useState('')
  const [isProcessing, setIsProcessing] = useState(false)

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('ru-RU', {
      style: 'currency',
      currency: 'RUB',
      maximumFractionDigits: 0,
    }).format(price)
  }

  const createOrder = useMutation({
    mutationFn: () => ordersApi.create(selectedPayment, cart?.promoCode, notes || undefined),
  })

  const handleCheckout = async () => {
    if (isProcessing) return

    if (!cart || cart.items.length === 0) {
      toast.error('Корзина пуста')
      return
    }

    setIsProcessing(true)

    let confirmed = false
    try {
      confirmed = await showConfirm(
        `Оформить заказ на сумму ${formatPrice(cart.total)}?`
      )
    } catch {
      // Popup already open or other error
      toast.error('Закройте текущее окно')
      setIsProcessing(false)
      return
    }

    if (!confirmed) {
      setIsProcessing(false)
      return
    }

    try {
      // Create order
      const order = await createOrder.mutateAsync()
      notificationOccurred('success')

      // Create payment
      let paymentResult
      switch (selectedPayment) {
        case 'YooKassa':
          paymentResult = await paymentsApi.createYooKassa(order.id, window.location.origin + '/payment/success')
          break
        case 'Robokassa':
          paymentResult = await paymentsApi.createRobokassa(order.id, window.location.origin + '/payment/success')
          break
        case 'TelegramStars':
          paymentResult = await paymentsApi.createTelegramStars(order.id)
          break
      }

      // Clear cart
      await clearCart()

      // Redirect to payment
      if (paymentResult?.confirmationUrl) {
        if (selectedPayment === 'TelegramStars') {
          // Open Telegram Stars payment with native invoice
          WebApp.openInvoice(paymentResult.confirmationUrl, (status) => {
            setIsProcessing(false)
            if (status === 'paid') {
              toast.success('Оплата прошла успешно!')
              notificationOccurred('success')
            } else if (status === 'cancelled') {
              toast.error('Оплата отменена')
            } else if (status === 'failed') {
              toast.error('Ошибка оплаты')
            }
            navigate(`/orders/${order.id}`)
          })
          return // Don't setIsProcessing in finally for Stars
        } else {
          // Open external payment page
          WebApp.openLink(paymentResult.confirmationUrl)
        }
      }
      navigate(`/orders/${order.id}`)
      setIsProcessing(false)
    } catch (error: any) {
      notificationOccurred('error')
      toast.error(error.response?.data?.error || 'Ошибка оформления заказа')
      setIsProcessing(false)
    }
  }

  useMainButton(
    isProcessing ? 'Оформление...' : `Оплатить ${formatPrice(cart?.total ?? 0)}`,
    handleCheckout,
    {
      isVisible: true,
      isActive: !isProcessing && !!cart && cart.items.length > 0,
      isProgressVisible: isProcessing,
    }
  )

  if (!cart || cart.items.length === 0) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <p className="tg-hint">Корзина пуста</p>
      </div>
    )
  }

  return (
    <div className="p-4 pb-24">
      <h1 className="text-xl font-semibold mb-4">Оформление заказа</h1>

      {/* Order Summary */}
      <div className="tg-card mb-4">
        <h2 className="font-medium mb-3">Ваш заказ</h2>
        <div className="space-y-2">
          {cart.items.map((item) => (
            <div key={item.id} className="flex justify-between text-sm">
              <span className="truncate flex-1 mr-2">
                {item.serviceTitle} × {item.quantity}
              </span>
              <span className="font-medium">{formatPrice(item.totalPrice)}</span>
            </div>
          ))}

          <div className="border-t border-tg-secondary-bg pt-2 mt-2">
            <div className="flex justify-between">
              <span className="tg-hint">Подытог</span>
              <span>{formatPrice(cart.subTotal)}</span>
            </div>
            {cart.discountAmount && cart.discountAmount > 0 && (
              <div className="flex justify-between text-green-600">
                <span>Скидка</span>
                <span>-{formatPrice(cart.discountAmount)}</span>
              </div>
            )}
            <div className="flex justify-between font-bold text-lg mt-1">
              <span>Итого</span>
              <span className="text-tg-button">{formatPrice(cart.total)}</span>
            </div>
          </div>
        </div>
      </div>

      {/* Payment Method */}
      <div className="mb-4">
        <h2 className="font-medium mb-3">Способ оплаты</h2>
        <div className="space-y-2">
          {paymentMethods.map((method) => (
            <motion.button
              key={method.id}
              onClick={() => setSelectedPayment(method.id)}
              className={`w-full tg-card flex items-center gap-3 text-left transition-all ${
                selectedPayment === method.id
                  ? 'ring-2 ring-tg-button'
                  : ''
              }`}
              whileTap={{ scale: 0.98 }}
            >
              <span className="text-2xl">{method.icon}</span>
              <div className="flex-1">
                <div className="font-medium">{method.name}</div>
                <div className="text-xs tg-hint">{method.description}</div>
              </div>
              <div
                className={`w-5 h-5 rounded-full border-2 flex items-center justify-center ${
                  selectedPayment === method.id
                    ? 'border-tg-button bg-tg-button'
                    : 'border-tg-hint'
                }`}
              >
                {selectedPayment === method.id && (
                  <div className="w-2 h-2 rounded-full bg-white" />
                )}
              </div>
            </motion.button>
          ))}
        </div>
      </div>

      {/* Notes */}
      <div>
        <h2 className="font-medium mb-3">Комментарий к заказу</h2>
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="Укажите пожелания или дополнительную информацию..."
          className="tg-input min-h-[100px] resize-none"
          maxLength={500}
        />
        <p className="text-xs tg-hint mt-1 text-right">
          {notes.length}/500
        </p>
      </div>
    </div>
  )
}
