import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { servicesApi, categoriesApi } from '@/services/api'
import { useMainButton, useHapticFeedback, usePopup } from '@/hooks/useTelegram'
import toast from 'react-hot-toast'

interface ServiceForm {
  title: string
  description: string
  categoryId: string
  price: string
  priceType: 'Fixed' | 'Hourly'
  deliveryDays: string
  responseTimeHours: string
  imageUrls: string[]
  tags: string[]
}

export function CreateServicePage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { notificationOccurred } = useHapticFeedback()
  const { showConfirm } = usePopup()

  const isEditing = !!id

  const [form, setForm] = useState<ServiceForm>({
    title: '',
    description: '',
    categoryId: '',
    price: '',
    priceType: 'Fixed',
    deliveryDays: '3',
    responseTimeHours: '24',
    imageUrls: [],
    tags: [],
  })

  const [tagInput, setTagInput] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  // Load existing service for editing
  const { data: existingService } = useQuery({
    queryKey: ['service', id],
    queryFn: () => servicesApi.getById(id!),
    enabled: isEditing,
  })

  // Load categories
  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: categoriesApi.getAll,
  })

  // Populate form when editing
  useEffect(() => {
    if (existingService?.service) {
      const s = existingService.service
      setForm({
        title: s.title,
        description: s.description,
        categoryId: s.categoryId,
        price: s.price.toString(),
        priceType: s.priceType,
        deliveryDays: s.deliveryDays.toString(),
        responseTimeHours: s.responseTimeHours.toString(),
        imageUrls: s.images.map((img) => img.imageUrl),
        tags: s.tags,
      })
    }
  }, [existingService])

  const createService = useMutation({
    mutationFn: (data: typeof form) =>
      servicesApi.create({
        title: data.title,
        description: data.description,
        categoryId: data.categoryId,
        price: parseFloat(data.price),
        priceType: data.priceType,
        deliveryDays: parseInt(data.deliveryDays),
        responseTimeHours: parseInt(data.responseTimeHours),
        imageUrls: data.imageUrls,
        tags: data.tags,
      } as any),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] })
      notificationOccurred('success')
      toast.success('Услуга создана')
      navigate('/seller')
    },
    onError: (error: any) => {
      notificationOccurred('error')
      toast.error(error.response?.data?.error || 'Ошибка создания услуги')
    },
  })

  const updateService = useMutation({
    mutationFn: (data: typeof form) =>
      servicesApi.update(id!, {
        title: data.title,
        description: data.description,
        categoryId: data.categoryId,
        price: parseFloat(data.price),
        priceType: data.priceType,
        deliveryDays: parseInt(data.deliveryDays),
        responseTimeHours: parseInt(data.responseTimeHours),
        imageUrls: data.imageUrls,
        tags: data.tags,
      } as any),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] })
      queryClient.invalidateQueries({ queryKey: ['service', id] })
      notificationOccurred('success')
      toast.success('Услуга обновлена')
      navigate('/seller')
    },
    onError: (error: any) => {
      notificationOccurred('error')
      toast.error(error.response?.data?.error || 'Ошибка обновления услуги')
    },
  })

  const deleteService = useMutation({
    mutationFn: () => servicesApi.delete(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['services'] })
      notificationOccurred('success')
      toast.success('Услуга удалена')
      navigate('/seller')
    },
  })

  const handleSubmit = async () => {
    // Validation
    if (!form.title.trim()) {
      toast.error('Введите название услуги')
      return
    }
    if (!form.description.trim()) {
      toast.error('Введите описание услуги')
      return
    }
    if (!form.categoryId) {
      toast.error('Выберите категорию')
      return
    }
    if (!form.price || parseFloat(form.price) <= 0) {
      toast.error('Введите корректную цену')
      return
    }

    setIsSaving(true)
    try {
      if (isEditing) {
        await updateService.mutateAsync(form)
      } else {
        await createService.mutateAsync(form)
      }
    } finally {
      setIsSaving(false)
    }
  }

  const handleDelete = async () => {
    const confirmed = await showConfirm('Удалить эту услугу?')
    if (confirmed) {
      await deleteService.mutateAsync()
    }
  }

  const handleAddTag = () => {
    const tag = tagInput.trim()
    if (tag && !form.tags.includes(tag) && form.tags.length < 10) {
      setForm((prev) => ({ ...prev, tags: [...prev.tags, tag] }))
      setTagInput('')
    }
  }

  const handleRemoveTag = (tag: string) => {
    setForm((prev) => ({ ...prev, tags: prev.tags.filter((t) => t !== tag) }))
  }

  useMainButton(
    isSaving ? 'Сохранение...' : isEditing ? 'Сохранить изменения' : 'Создать услугу',
    handleSubmit,
    {
      isVisible: true,
      isActive: !isSaving,
      isProgressVisible: isSaving,
    }
  )

  const flatCategories = categories?.flatMap((c) =>
    c.children
      ? [c, ...c.children.map((child) => ({ ...child, name: `  ${child.name}` }))]
      : [c]
  )

  return (
    <div className="min-h-screen p-4 pb-24">
      <h1 className="text-xl font-semibold mb-4">
        {isEditing ? 'Редактировать услугу' : 'Новая услуга'}
      </h1>

      <div className="space-y-4">
        {/* Title */}
        <div>
          <label className="block text-sm font-medium mb-1">Название *</label>
          <input
            type="text"
            value={form.title}
            onChange={(e) => setForm((prev) => ({ ...prev, title: e.target.value }))}
            placeholder="Например: Создание логотипа"
            className="tg-input"
            maxLength={200}
          />
        </div>

        {/* Description */}
        <div>
          <label className="block text-sm font-medium mb-1">Описание *</label>
          <textarea
            value={form.description}
            onChange={(e) => setForm((prev) => ({ ...prev, description: e.target.value }))}
            placeholder="Подробно опишите вашу услугу..."
            className="tg-input min-h-[120px] resize-none"
            maxLength={5000}
          />
          <p className="text-xs tg-hint mt-1 text-right">{form.description.length}/5000</p>
        </div>

        {/* Category */}
        <div>
          <label className="block text-sm font-medium mb-1">Категория *</label>
          <select
            value={form.categoryId}
            onChange={(e) => setForm((prev) => ({ ...prev, categoryId: e.target.value }))}
            className="tg-input"
          >
            <option value="">Выберите категорию</option>
            {flatCategories?.map((cat) => (
              <option key={cat.id} value={cat.id}>
                {cat.name}
              </option>
            ))}
          </select>
        </div>

        {/* Price */}
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-sm font-medium mb-1">Цена (₽) *</label>
            <input
              type="number"
              value={form.price}
              onChange={(e) => setForm((prev) => ({ ...prev, price: e.target.value }))}
              placeholder="1000"
              className="tg-input"
              min="1"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Тип цены</label>
            <select
              value={form.priceType}
              onChange={(e) => setForm((prev) => ({ ...prev, priceType: e.target.value as any }))}
              className="tg-input"
            >
              <option value="Fixed">Фиксированная</option>
              <option value="Hourly">Почасовая</option>
            </select>
          </div>
        </div>

        {/* Delivery & Response Time */}
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-sm font-medium mb-1">Срок выполнения (дней)</label>
            <input
              type="number"
              value={form.deliveryDays}
              onChange={(e) => setForm((prev) => ({ ...prev, deliveryDays: e.target.value }))}
              placeholder="3"
              className="tg-input"
              min="1"
              max="365"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Время ответа (часов)</label>
            <input
              type="number"
              value={form.responseTimeHours}
              onChange={(e) => setForm((prev) => ({ ...prev, responseTimeHours: e.target.value }))}
              placeholder="24"
              className="tg-input"
              min="1"
              max="168"
            />
          </div>
        </div>

        {/* Tags */}
        <div>
          <label className="block text-sm font-medium mb-1">Теги</label>
          <div className="flex gap-2">
            <input
              type="text"
              value={tagInput}
              onChange={(e) => setTagInput(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), handleAddTag())}
              placeholder="Добавить тег"
              className="tg-input flex-1"
              maxLength={30}
            />
            <button onClick={handleAddTag} className="tg-button-secondary px-4">
              +
            </button>
          </div>
          {form.tags.length > 0 && (
            <div className="flex flex-wrap gap-2 mt-2">
              {form.tags.map((tag) => (
                <span
                  key={tag}
                  className="px-3 py-1 bg-tg-secondary-bg rounded-full text-sm flex items-center gap-1"
                >
                  {tag}
                  <button onClick={() => handleRemoveTag(tag)} className="ml-1">
                    ×
                  </button>
                </span>
              ))}
            </div>
          )}
        </div>

        {/* Image URLs (simplified - in production would use file upload) */}
        <div>
          <label className="block text-sm font-medium mb-1">Ссылки на изображения</label>
          <textarea
            value={form.imageUrls.join('\n')}
            onChange={(e) =>
              setForm((prev) => ({
                ...prev,
                imageUrls: e.target.value.split('\n').filter(Boolean),
              }))
            }
            placeholder="Введите URL изображений (по одному на строку)"
            className="tg-input min-h-[80px] resize-none"
          />
          <p className="text-xs tg-hint mt-1">Максимум 10 изображений</p>
        </div>

        {/* Delete Button (for editing) */}
        {isEditing && (
          <button
            onClick={handleDelete}
            className="w-full py-3 text-tg-destructive font-medium"
          >
            Удалить услугу
          </button>
        )}
      </div>
    </div>
  )
}
