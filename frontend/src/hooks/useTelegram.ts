import { useEffect, useCallback } from 'react'
import WebApp from '@twa-dev/sdk'
import { useNavigate, useLocation } from 'react-router-dom'

export function useTelegram() {
  return {
    webApp: WebApp,
    user: WebApp.initDataUnsafe?.user,
    colorScheme: WebApp.colorScheme,
    themeParams: WebApp.themeParams,
    isExpanded: WebApp.isExpanded,
    viewportHeight: WebApp.viewportHeight,
    viewportStableHeight: WebApp.viewportStableHeight,
    headerColor: WebApp.headerColor,
    backgroundColor: WebApp.backgroundColor,
    isClosingConfirmationEnabled: WebApp.isClosingConfirmationEnabled,
    platform: WebApp.platform,
    version: WebApp.version,
  }
}

export function useMainButton(
  text: string,
  onClick: () => void,
  options?: {
    isVisible?: boolean
    isActive?: boolean
    isProgressVisible?: boolean
    color?: string
    textColor?: string
  }
) {
  useEffect(() => {
    // Don't configure button if it's not visible or text is empty
    if (options?.isVisible === false || !text) {
      WebApp.MainButton.hide()
      return () => {
        WebApp.MainButton.offClick(onClick)
      }
    }

    WebApp.MainButton.setText(text)

    if (options?.color) {
      WebApp.MainButton.color = options.color as `#${string}`
    }
    if (options?.textColor) {
      WebApp.MainButton.textColor = options.textColor as `#${string}`
    }

    WebApp.MainButton.onClick(onClick)
    WebApp.MainButton.show()

    if (options?.isActive === false) {
      WebApp.MainButton.disable()
    } else {
      WebApp.MainButton.enable()
    }

    if (options?.isProgressVisible) {
      WebApp.MainButton.showProgress()
    } else {
      WebApp.MainButton.hideProgress()
    }

    return () => {
      WebApp.MainButton.offClick(onClick)
      WebApp.MainButton.hide()
    }
  }, [text, onClick, options])
}

export function useBackButton() {
  const navigate = useNavigate()
  const location = useLocation()

  useEffect(() => {
    const isHome = location.pathname === '/'

    if (isHome) {
      WebApp.BackButton.hide()
    } else {
      WebApp.BackButton.show()
    }

    const handleBack = () => {
      navigate(-1)
    }

    WebApp.BackButton.onClick(handleBack)

    return () => {
      WebApp.BackButton.offClick(handleBack)
    }
  }, [location.pathname, navigate])
}

export function useHapticFeedback() {
  const impactOccurred = useCallback((style: 'light' | 'medium' | 'heavy' | 'rigid' | 'soft') => {
    WebApp.HapticFeedback.impactOccurred(style)
  }, [])

  const notificationOccurred = useCallback((type: 'error' | 'success' | 'warning') => {
    WebApp.HapticFeedback.notificationOccurred(type)
  }, [])

  const selectionChanged = useCallback(() => {
    WebApp.HapticFeedback.selectionChanged()
  }, [])

  return { impactOccurred, notificationOccurred, selectionChanged }
}

export function usePopup() {
  const showAlert = useCallback((message: string): Promise<void> => {
    return new Promise((resolve) => {
      WebApp.showAlert(message, resolve)
    })
  }, [])

  const showConfirm = useCallback((message: string): Promise<boolean> => {
    return new Promise((resolve) => {
      WebApp.showConfirm(message, resolve)
    })
  }, [])

  const showPopup = useCallback((params: {
    title?: string
    message: string
    buttons?: Array<{
      id?: string
      type?: 'default' | 'ok' | 'close' | 'cancel' | 'destructive'
      text?: string
    }>
  }): Promise<string | null> => {
    return new Promise((resolve) => {
      WebApp.showPopup(params as Parameters<typeof WebApp.showPopup>[0], (buttonId) => {
        resolve(buttonId ?? null)
      })
    })
  }, [])

  return { showAlert, showConfirm, showPopup }
}

export function useCloudStorage() {
  const setItem = useCallback((key: string, value: string): Promise<void> => {
    return new Promise((resolve, reject) => {
      WebApp.CloudStorage.setItem(key, value, (error, result) => {
        if (error || !result) {
          reject(new Error('Failed to set item'))
        } else {
          resolve()
        }
      })
    })
  }, [])

  const getItem = useCallback((key: string): Promise<string | null> => {
    return new Promise((resolve, reject) => {
      WebApp.CloudStorage.getItem(key, (error, value) => {
        if (error) {
          reject(new Error('Failed to get item'))
        } else {
          resolve(value ?? null)
        }
      })
    })
  }, [])

  const removeItem = useCallback((key: string): Promise<void> => {
    return new Promise((resolve, reject) => {
      WebApp.CloudStorage.removeItem(key, (error, result) => {
        if (error || !result) {
          reject(new Error('Failed to remove item'))
        } else {
          resolve()
        }
      })
    })
  }, [])

  return { setItem, getItem, removeItem }
}
