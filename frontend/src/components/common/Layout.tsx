import { ReactNode } from 'react'
import { useBackButton } from '@/hooks/useTelegram'
import { BottomNavigation } from './BottomNavigation'

interface LayoutProps {
  children: ReactNode
}

export function Layout({ children }: LayoutProps) {
  useBackButton()

  return (
    <div className="min-h-screen flex flex-col bg-tg-bg">
      <main className="flex-1 pb-20">
        {children}
      </main>
      <BottomNavigation />
    </div>
  )
}
