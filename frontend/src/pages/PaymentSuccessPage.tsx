import { useEffect } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { motion } from 'framer-motion'
import { CheckIcon } from '@/components/common/Icons'
import WebApp from '@twa-dev/sdk'

export function PaymentSuccessPage() {
  const [searchParams] = useSearchParams()
  const orderId = searchParams.get('orderId')

  useEffect(() => {
    // Haptic feedback for success
    WebApp.HapticFeedback.notificationOccurred('success')
  }, [])

  return (
    <div className="min-h-screen flex flex-col items-center justify-center p-8 text-center">
      <motion.div
        initial={{ scale: 0 }}
        animate={{ scale: 1 }}
        transition={{ type: 'spring', duration: 0.5 }}
        className="w-20 h-20 rounded-full bg-green-500 flex items-center justify-center mb-6"
      >
        <CheckIcon className="w-10 h-10 text-white" />
      </motion.div>

      <motion.h1
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.2 }}
        className="text-2xl font-bold mb-2"
      >
        Оплата успешна!
      </motion.h1>

      <motion.p
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.3 }}
        className="tg-hint mb-8"
      >
        Ваш заказ оплачен и передан продавцу
      </motion.p>

      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.4 }}
        className="space-y-3 w-full"
      >
        {orderId && (
          <Link to={`/orders/${orderId}`} className="tg-button w-full block">
            Перейти к заказу
          </Link>
        )}
        <Link to="/orders" className="tg-button-secondary w-full block">
          Мои заказы
        </Link>
        <Link to="/" className="block py-3 tg-link">
          На главную
        </Link>
      </motion.div>
    </div>
  )
}
