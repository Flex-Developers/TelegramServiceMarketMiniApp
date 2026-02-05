import { useState } from 'react'
import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { useAuthStore } from '@/store/authStore'
import { ChevronRightIcon } from '@/components/common/Icons'
import { usersApi } from '@/services/api'
import toast from 'react-hot-toast'

export function ProfilePage() {
  const { user, isAuthenticated, logout, updateUser } = useAuthStore()
  const [isBecomingSelller, setIsBecomingSeller] = useState(false)

  if (!isAuthenticated || !user) {
    return (
      <div className="flex items-center justify-center min-h-[60vh] p-4">
        <div className="text-center">
          <div className="text-5xl mb-4">👤</div>
          <h2 className="text-lg font-semibold mb-2">Войдите в аккаунт</h2>
          <p className="tg-hint">
            Авторизуйтесь через Telegram для доступа к профилю
          </p>
        </div>
      </div>
    )
  }

  const handleBecomeSeller = async () => {
    if (isBecomingSelller) return
    setIsBecomingSeller(true)
    try {
      const result = await usersApi.becomeSeller()
      updateUser({ role: result.role as 'Buyer' | 'Seller' | 'Both' | 'Admin' })
      toast.success('Теперь вы продавец! Можете добавлять услуги.')
    } catch (error) {
      toast.error('Не удалось стать продавцом')
      console.error(error)
    } finally {
      setIsBecomingSeller(false)
    }
  }

  // Role can be string or number: Buyer=0, Seller=1, Both=2, Admin=3
  const roleStr = String(user.role)
  const isBuyer = roleStr === 'Buyer' || roleStr === '0'
  const isSeller = !isBuyer
  const isAdmin = roleStr === 'Admin' || roleStr === '3'
  console.log('User role:', user.role, 'isBuyer:', isBuyer, 'isSeller:', isSeller, 'isAdmin:', isAdmin)

  const menuItems = [
    { path: '/orders', icon: '📦', label: 'Мои заказы' },
    { path: '/favorites', icon: '❤️', label: 'Избранное' },
    {
      path: '/seller',
      icon: '💼',
      label: 'Кабинет продавца',
      condition: isSeller,
    },
    {
      path: '/seller/services/new',
      icon: '➕',
      label: 'Создать услугу',
      condition: isSeller,
    },
    {
      path: '/admin/categories',
      icon: '⚙️',
      label: 'Управление категориями',
      condition: isAdmin,
    },
  ].filter((item) => item.condition !== false)

  const getRoleLabel = (role: string) => {
    switch (role) {
      case 'Buyer':
        return 'Покупатель'
      case 'Seller':
        return 'Продавец'
      case 'Both':
        return 'Покупатель и продавец'
      case 'Admin':
        return 'Администратор'
      default:
        return role
    }
  }

  return (
    <div className="min-h-screen pb-20">
      {/* Profile Header */}
      <div className="bg-gradient-to-b from-tg-button/10 to-transparent p-6">
        <div className="flex items-center gap-4">
          {user.photoUrl ? (
            <img
              src={user.photoUrl}
              alt={user.firstName}
              className="w-20 h-20 rounded-full border-4 border-white shadow-lg"
            />
          ) : (
            <div className="w-20 h-20 rounded-full bg-tg-button flex items-center justify-center text-white text-2xl font-bold border-4 border-white shadow-lg">
              {user.firstName[0]}
            </div>
          )}

          <div>
            <h1 className="text-xl font-bold">
              {user.firstName} {user.lastName}
            </h1>
            {user.username && (
              <p className="tg-hint">@{user.username}</p>
            )}
            <div className="flex items-center gap-2 mt-1">
              <span className="text-xs px-2 py-0.5 bg-tg-button/10 text-tg-button rounded-full">
                {getRoleLabel(user.role)}
              </span>
              {user.isVerified && (
                <span className="text-xs px-2 py-0.5 bg-green-100 text-green-800 rounded-full">
                  Верифицирован ✓
                </span>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Menu */}
      <div className="p-4">
        <div className="space-y-2">
          {menuItems.map((item, index) => (
            <motion.div
              key={item.path}
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ duration: 0.2, delay: index * 0.05 }}
            >
              <Link
                to={item.path}
                className="tg-card flex items-center justify-between active:scale-[0.98] transition-transform"
              >
                <div className="flex items-center gap-3">
                  <span className="text-2xl">{item.icon}</span>
                  <span className="font-medium">{item.label}</span>
                </div>
                <ChevronRightIcon className="w-5 h-5 tg-hint" />
              </Link>
            </motion.div>
          ))}
        </div>

        {/* Become Seller Button */}
        <motion.button
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.2, delay: 0.15 }}
          onClick={handleBecomeSeller}
          disabled={isBecomingSelller || !isBuyer}
          className="w-full mt-4 tg-button flex items-center justify-center gap-2 disabled:opacity-50"
        >
          {isBecomingSelller ? (
            <>
              <span className="animate-spin">⏳</span>
              Обработка...
            </>
          ) : isBuyer ? (
            <>
              <span>💼</span>
              Стать продавцом
            </>
          ) : (
            <>
              <span>✅</span>
              Вы уже продавец
            </>
          )}
        </motion.button>

        {/* Info */}
        <div className="tg-card mt-6">
          <h3 className="font-medium mb-3">Информация</h3>
          <div className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="tg-hint">Telegram ID</span>
              <span>{user.telegramId}</span>
            </div>
            <div className="flex justify-between">
              <span className="tg-hint">Язык</span>
              <span>
                {user.languageCode === 'ru' ? 'Русский' :
                 user.languageCode === 'en' ? 'English' :
                 user.languageCode === 'de' ? 'Deutsch' :
                 user.languageCode || 'Русский'}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="tg-hint">Дата регистрации</span>
              <span>
                {new Date(user.createdAt).toLocaleDateString('ru-RU', {
                  day: 'numeric',
                  month: 'long',
                  year: 'numeric',
                })}
              </span>
            </div>
          </div>
        </div>

        {/* Logout */}
        <button
          onClick={logout}
          className="w-full tg-button-secondary mt-6 text-tg-destructive"
        >
          Выйти из аккаунта
        </button>
      </div>
    </div>
  )
}
