import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center p-8 text-center">
      <div className="text-6xl mb-4">🔍</div>
      <h1 className="text-2xl font-bold mb-2">Страница не найдена</h1>
      <p className="tg-hint mb-6">
        Возможно, она была перемещена или удалена
      </p>
      <Link to="/" className="tg-button">
        На главную
      </Link>
    </div>
  )
}
