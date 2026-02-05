import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ordersApi } from '@/services/api'
import { useAuthStore } from '@/store/authStore'
import { useMainButton, useHapticFeedback, usePopup } from '@/hooks/useTelegram'
import type { OrderStatus } from '@/types'
import toast from 'react-hot-toast'

const statusLabels: Record<OrderStatus, { label: string; color: string }> = {
  Pending: { label: 'Ожидает оплаты', color: 'bg-yellow-100 text-yellow-800' },
  Paid: { label: 'Оплачен', color: 'bg-blue-100 text-blue-800' },
  Processing: { label: 'В работе', color: 'bg-purple-100 text-purple-800' },
  Delivered: { label: 'Доставлен', color: 'bg-green-100 text-green-800' },
  Completed: { label: 'Завершён', color: 'bg-green-100 text-green-800' },
  Cancelled: { label: 'Отменён', color: 'bg-red-100 text-red-800' },
  Refunded: { label: 'Возврат', color: 'bg-gray-100 text-gray-800' },
  Disputed: { label: 'Спор', color: 'bg-orange-100 text-orange-800' },
}

export function OrderDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuthStore()
  const { notificationOccurred } = useHapticFeedback()
  const { showConfirm, showPopup } = usePopup()

  const { data: order, isLoading } = useQuery({
    queryKey: ['order', id],
    queryFn: () => ordersApi.getById(id!),
    enabled: !!id,
  })

  const updateStatus = useMutation({
    mutationFn: (status: string) => ordersApi.updateStatus(id!, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['order', id] })
      queryClient.invalidateQueries({ queryKey: ['orders'] })
      notificationOccurred('success')
      toast.success('Статус обновлён')
    },
    onError: (error: any) => {
      notificationOccurred('error')
      toast.error(error.response?.data?.error || 'Ошибка обновления статуса')
    },
  })

  const cancelOrder = useMutation({
    mutationFn: (reason: string) => ordersApi.cancel(id!, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['order', id] })
      queryClient.invalidateQueries({ queryKey: ['orders'] })
      notificationOccurred('success')
      toast.success('Заказ отменён')
    },
  })

  const isSeller = order && user?.id === order.sellerId
  const isBuyer = order && user?.id === order.buyerId
  const [isPopupOpen, setIsPopupOpen] = useState(false)

  const handleAction = async () => {
    if (!order || isPopupOpen) return
    setIsPopupOpen(true)

    try {
      if (isSeller) {
        if (order.status === 'Paid') {
          const confirmed = await showConfirm('Начать выполнение заказа?')
          if (confirmed) {
            await updateStatus.mutateAsync('Processing')
          }
        } else if (order.status === 'Processing') {
          const confirmed = await showConfirm('Отметить заказ как доставленный?')
          if (confirmed) {
            await updateStatus.mutateAsync('Delivered')
          }
        }
      } else if (isBuyer) {
        if (order.status === 'Delivered') {
          const confirmed = await showConfirm('Подтвердить получение заказа?')
          if (confirmed) {
            await updateStatus.mutateAsync('Completed')
            navigate(`/reviews/create?orderId=${order.id}`)
          }
        }
      }
    } catch {
      toast.error('Закройте текущее окно')
    } finally {
      setIsPopupOpen(false)
    }
  }

  const handleCancel = async () => {
    if (isPopupOpen) return
    setIsPopupOpen(true)

    try {
      const confirmed = await showConfirm('Отменить заказ?')
      if (confirmed) {
        await cancelOrder.mutateAsync('Отменено покупателем')
      }
    } catch {
      toast.error('Закройте текущее окно')
    } finally {
      setIsPopupOpen(false)
    }
  }

  // Determine main button text and action
  let mainButtonText = ''
  let mainButtonVisible = false

  if (order) {
    if (isSeller && order.status === 'Paid') {
      mainButtonText = 'Начать выполнение'
      mainButtonVisible = true
    } else if (isSeller && order.status === 'Processing') {
      mainButtonText = 'Отметить доставленным'
      mainButtonVisible = true
    } else if (isBuyer && order.status === 'Delivered') {
      mainButtonText = 'Подтвердить получение'
      mainButtonVisible = true
    }
  }

  useMainButton(mainButtonText, handleAction, {
    isVisible: mainButtonVisible,
    isActive: !updateStatus.isPending,
  })

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('ru-RU', {
      style: 'currency',
      currency: 'RUB',
      maximumFractionDigits: 0,
    }).format(price)
  }

  const formatDate = (date: string) => {
    return new Date(date).toLocaleString('ru-RU', {
      day: 'numeric',
      month: 'long',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="w-8 h-8 border-2 border-tg-button border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  if (!order) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <p className="tg-hint">Заказ не найден</p>
      </div>
    )
  }

  return (
    <div className="min-h-screen pb-24">
      {/* Header */}
      <div className="p-4 border-b border-tg-secondary-bg">
        <div className="flex items-center justify-between mb-2">
          <h1 className="text-lg font-semibold">
            Заказ #{order.id.slice(0, 8)}
          </h1>
          <span className={`px-3 py-1 rounded-full text-sm ${statusLabels[order.status].color}`}>
            {statusLabels[order.status].label}
          </span>
        </div>
        <p className="text-sm tg-hint">{formatDate(order.createdAt)}</p>
      </div>

      <div className="p-4 space-y-4">
        {/* Other Party */}
        <div className="tg-card">
          <h3 className="text-sm tg-hint mb-2">
            {isSeller ? 'Покупатель' : 'Продавец'}
          </h3>
          <div className="flex items-center gap-3">
            {(isSeller ? order.buyer : order.seller).photoUrl ? (
              <img
                src={(isSeller ? order.buyer : order.seller).photoUrl}
                alt=""
                className="w-10 h-10 rounded-full"
              />
            ) : (
              <div className="w-10 h-10 rounded-full bg-tg-button flex items-center justify-center text-white font-medium">
                {(isSeller ? order.buyer : order.seller).firstName[0]}
              </div>
            )}
            <div>
              <div className="font-medium">
                {(isSeller ? order.buyer : order.seller).firstName}
              </div>
              {(isSeller ? order.buyer : order.seller).username && (
                <div className="text-sm tg-hint">
                  @{(isSeller ? order.buyer : order.seller).username}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Order Items */}
        <div className="tg-card">
          <h3 className="text-sm tg-hint mb-3">Состав заказа</h3>
          <div className="space-y-3">
            {order.items.map((item) => (
              <div key={item.id} className="flex gap-3">
                {item.thumbnailUrl ? (
                  <img
                    src={item.thumbnailUrl}
                    alt={item.serviceTitle}
                    className="w-14 h-14 rounded-lg object-cover"
                  />
                ) : (
                  <div className="w-14 h-14 rounded-lg bg-tg-secondary-bg flex items-center justify-center">
                    📦
                  </div>
                )}
                <div className="flex-1">
                  <div className="font-medium text-sm">{item.serviceTitle}</div>
                  <div className="text-sm tg-hint">× {item.quantity}</div>
                </div>
                <div className="font-medium">{formatPrice(item.totalPrice)}</div>
              </div>
            ))}
          </div>
        </div>

        {/* Payment Details */}
        <div className="tg-card">
          <h3 className="text-sm tg-hint mb-3">Оплата</h3>
          <div className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="tg-hint">Подытог</span>
              <span>{formatPrice(order.subTotal)}</span>
            </div>
            {order.discountAmount > 0 && (
              <div className="flex justify-between text-green-600">
                <span>Скидка</span>
                <span>-{formatPrice(order.discountAmount)}</span>
              </div>
            )}
            <div className="flex justify-between font-bold text-lg pt-2 border-t border-tg-secondary-bg">
              <span>Итого</span>
              <span className="text-tg-button">{formatPrice(order.totalAmount)}</span>
            </div>
          </div>
        </div>

        {/* Notes */}
        {order.notes && (
          <div className="tg-card">
            <h3 className="text-sm tg-hint mb-2">Комментарий</h3>
            <p className="text-sm">{order.notes}</p>
          </div>
        )}

        {/* Cancellation Info */}
        {order.status === 'Cancelled' && order.cancellationReason && (
          <div className="tg-card bg-red-50">
            <h3 className="text-sm text-red-800 mb-2">Причина отмены</h3>
            <p className="text-sm text-red-700">{order.cancellationReason}</p>
          </div>
        )}

        {/* Cancel Button */}
        {(order.status === 'Pending' || order.status === 'Paid') && (
          <button
            onClick={handleCancel}
            className="w-full py-3 text-tg-destructive font-medium"
          >
            Отменить заказ
          </button>
        )}
      </div>
    </div>
  )
}
