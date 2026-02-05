import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { categoriesApi } from '@/services/api'
import { useAuthStore } from '@/store/authStore'
import toast from 'react-hot-toast'
import type { Category } from '@/types'

export function AdminCategoriesPage() {
  const { user } = useAuthStore()
  const queryClient = useQueryClient()
  const [isCreating, setIsCreating] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [formData, setFormData] = useState({
    name: '',
    nameEn: '',
    nameDe: '',
    icon: '',
    sortOrder: 0,
  })

  const { data: categories, isLoading } = useQuery({
    queryKey: ['categories', 'all'],
    queryFn: categoriesApi.getAll,
  })

  const createMutation = useMutation({
    mutationFn: categoriesApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] })
      toast.success('Категория создана')
      resetForm()
    },
    onError: () => toast.error('Ошибка создания категории'),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: typeof formData }) =>
      categoriesApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] })
      toast.success('Категория обновлена')
      resetForm()
    },
    onError: () => toast.error('Ошибка обновления'),
  })

  const deleteMutation = useMutation({
    mutationFn: categoriesApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] })
      toast.success('Категория удалена')
    },
    onError: () => toast.error('Ошибка удаления'),
  })

  const toggleActiveMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      isActive ? categoriesApi.deactivate(id) : categoriesApi.activate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['categories'] })
    },
  })

  const roleStr = String(user?.role ?? '')
  const isAdmin = roleStr === 'Admin' || roleStr === '3'

  if (!isAdmin) {
    return (
      <div className="flex items-center justify-center min-h-[60vh] p-4">
        <div className="text-center">
          <div className="text-5xl mb-4">🔒</div>
          <h2 className="text-lg font-semibold mb-2">Доступ запрещён</h2>
          <p className="tg-hint">Эта страница доступна только администраторам</p>
        </div>
      </div>
    )
  }

  const resetForm = () => {
    setFormData({ name: '', nameEn: '', nameDe: '', icon: '', sortOrder: 0 })
    setIsCreating(false)
    setEditingId(null)
  }

  const handleEdit = (category: Category) => {
    setFormData({
      name: category.name,
      nameEn: category.nameEn || '',
      nameDe: category.nameDe || '',
      icon: category.icon || '',
      sortOrder: category.sortOrder || 0,
    })
    setEditingId(category.id)
    setIsCreating(true)
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!formData.name.trim()) {
      toast.error('Введите название категории')
      return
    }

    if (editingId) {
      updateMutation.mutate({ id: editingId, data: formData })
    } else {
      createMutation.mutate(formData)
    }
  }

  const handleDelete = (id: string, name: string) => {
    if (confirm(`Удалить категорию "${name}"?`)) {
      deleteMutation.mutate(id)
    }
  }

  return (
    <div className="min-h-screen pb-20">
      {/* Header */}
      <div className="bg-gradient-to-b from-tg-button/10 to-transparent p-4">
        <h1 className="text-xl font-bold mb-2">Управление категориями</h1>
        <p className="tg-hint text-sm">Создание и редактирование категорий услуг</p>
      </div>

      <div className="p-4 space-y-4">
        {/* Create/Edit Form */}
        {isCreating ? (
          <motion.form
            initial={{ opacity: 0, y: -10 }}
            animate={{ opacity: 1, y: 0 }}
            className="tg-card space-y-3"
            onSubmit={handleSubmit}
          >
            <h3 className="font-semibold">
              {editingId ? 'Редактирование' : 'Новая категория'}
            </h3>

            <input
              type="text"
              placeholder="Название (RU) *"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              className="tg-input w-full"
            />

            <div className="grid grid-cols-2 gap-2">
              <input
                type="text"
                placeholder="Name (EN)"
                value={formData.nameEn}
                onChange={(e) => setFormData({ ...formData, nameEn: e.target.value })}
                className="tg-input"
              />
              <input
                type="text"
                placeholder="Name (DE)"
                value={formData.nameDe}
                onChange={(e) => setFormData({ ...formData, nameDe: e.target.value })}
                className="tg-input"
              />
            </div>

            <div className="grid grid-cols-2 gap-2">
              <input
                type="text"
                placeholder="Иконка (emoji)"
                value={formData.icon}
                onChange={(e) => setFormData({ ...formData, icon: e.target.value })}
                className="tg-input"
              />
              <input
                type="number"
                placeholder="Порядок"
                value={formData.sortOrder}
                onChange={(e) => setFormData({ ...formData, sortOrder: parseInt(e.target.value) || 0 })}
                className="tg-input"
              />
            </div>

            <div className="flex gap-2">
              <button
                type="submit"
                disabled={createMutation.isPending || updateMutation.isPending}
                className="tg-button flex-1"
              >
                {createMutation.isPending || updateMutation.isPending
                  ? 'Сохранение...'
                  : editingId
                  ? 'Сохранить'
                  : 'Создать'}
              </button>
              <button
                type="button"
                onClick={resetForm}
                className="tg-button-secondary"
              >
                Отмена
              </button>
            </div>
          </motion.form>
        ) : (
          <button
            onClick={() => setIsCreating(true)}
            className="tg-button w-full flex items-center justify-center gap-2"
          >
            <span>➕</span>
            Добавить категорию
          </button>
        )}

        {/* Categories List */}
        <div className="space-y-2">
          <h3 className="font-semibold">Категории ({categories?.length || 0})</h3>

          {isLoading ? (
            <div className="tg-card text-center py-8">Загрузка...</div>
          ) : categories?.length === 0 ? (
            <div className="tg-card text-center py-8 tg-hint">
              Нет категорий. Создайте первую!
            </div>
          ) : (
            categories?.map((category, index) => (
              <motion.div
                key={category.id}
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: index * 0.05 }}
                className={`tg-card flex items-center justify-between ${
                  !category.isActive ? 'opacity-50' : ''
                }`}
              >
                <div className="flex items-center gap-3">
                  <span className="text-2xl">{category.icon || '📁'}</span>
                  <div>
                    <div className="font-medium">{category.name}</div>
                    <div className="text-xs tg-hint">
                      {category.serviceCount || 0} услуг • Порядок: {category.sortOrder}
                    </div>
                  </div>
                </div>

                <div className="flex items-center gap-1">
                  <button
                    onClick={() => toggleActiveMutation.mutate({
                      id: category.id,
                      isActive: category.isActive
                    })}
                    className="p-2 hover:bg-tg-secondary-bg rounded-lg transition-colors"
                    title={category.isActive ? 'Деактивировать' : 'Активировать'}
                  >
                    {category.isActive ? '👁' : '👁‍🗨'}
                  </button>
                  <button
                    onClick={() => handleEdit(category)}
                    className="p-2 hover:bg-tg-secondary-bg rounded-lg transition-colors"
                    title="Редактировать"
                  >
                    ✏️
                  </button>
                  <button
                    onClick={() => handleDelete(category.id, category.name)}
                    className="p-2 hover:bg-red-100 rounded-lg transition-colors text-red-500"
                    title="Удалить"
                  >
                    🗑
                  </button>
                </div>
              </motion.div>
            ))
          )}
        </div>
      </div>
    </div>
  )
}
