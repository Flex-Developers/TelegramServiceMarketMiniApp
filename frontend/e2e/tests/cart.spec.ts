import { test, expect } from '@playwright/test'

test.describe('Cart Page', () => {
  test.beforeEach(async ({ page }) => {
    // Mock Telegram WebApp
    await page.addInitScript(() => {
      (window as any).Telegram = {
        WebApp: {
          ready: () => {},
          expand: () => {},
          enableClosingConfirmation: () => {},
          colorScheme: 'light',
          themeParams: {},
          initData: 'mock_init_data',
          initDataUnsafe: {
            user: {
              id: 123456789,
              first_name: 'Test',
              last_name: 'User',
              username: 'testuser',
            },
          },
          MainButton: {
            text: '',
            setText: () => {},
            show: () => {},
            hide: () => {},
            onClick: () => {},
            offClick: () => {},
            enable: () => {},
            disable: () => {},
            showProgress: () => {},
            hideProgress: () => {},
          },
          BackButton: {
            show: () => {},
            hide: () => {},
            onClick: () => {},
            offClick: () => {},
          },
          HapticFeedback: {
            impactOccurred: () => {},
            notificationOccurred: () => {},
            selectionChanged: () => {},
          },
          openLink: () => {},
          close: () => {},
        },
      }
    })
  })

  test('should show empty cart message', async ({ page }) => {
    // Mock empty cart API
    await page.route('**/api/cart', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [],
          subTotal: 0,
          total: 0,
          itemCount: 0,
        }),
      })
    })

    await page.goto('/cart')

    await expect(page.locator('text=Корзина пуста')).toBeVisible()
    await expect(page.locator('text=Перейти в каталог')).toBeVisible()
  })

  test('should display cart items', async ({ page }) => {
    // Mock cart with items
    await page.route('**/api/cart', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'cart-item-1',
              serviceId: 'service-1',
              serviceTitle: 'Дизайн логотипа',
              servicePrice: 5000,
              thumbnailUrl: null,
              quantity: 1,
              totalPrice: 5000,
              seller: {
                id: 'seller-1',
                firstName: 'Иван',
                isVerified: true,
                averageRating: 4.8,
              },
            },
          ],
          subTotal: 5000,
          total: 5000,
          itemCount: 1,
        }),
      })
    })

    await page.goto('/cart')

    await expect(page.locator('text=Дизайн логотипа')).toBeVisible()
    await expect(page.locator('text=5 000')).toBeVisible()
  })

  test('should update item quantity', async ({ page }) => {
    let currentQuantity = 1

    // Mock cart
    await page.route('**/api/cart', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'cart-item-1',
              serviceId: 'service-1',
              serviceTitle: 'Тестовая услуга',
              servicePrice: 1000,
              quantity: currentQuantity,
              totalPrice: 1000 * currentQuantity,
              seller: { id: 's1', firstName: 'Тест', isVerified: false, averageRating: 4 },
            },
          ],
          subTotal: 1000 * currentQuantity,
          total: 1000 * currentQuantity,
          itemCount: currentQuantity,
        }),
      })
    })

    // Mock update
    await page.route('**/api/cart/items/*', async (route) => {
      currentQuantity = 2
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'cart-item-1',
          quantity: 2,
          totalPrice: 2000,
        }),
      })
    })

    await page.goto('/cart')

    // Click plus button to increase quantity
    await page.locator('button:has(svg) >> nth=1').click()

    // Wait for update
    await page.waitForTimeout(500)
  })

  test('should apply promo code', async ({ page }) => {
    // Mock cart
    await page.route('**/api/cart', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'item-1',
              serviceId: 's-1',
              serviceTitle: 'Услуга',
              servicePrice: 10000,
              quantity: 1,
              totalPrice: 10000,
              seller: { id: 's1', firstName: 'Продавец', isVerified: true, averageRating: 5 },
            },
          ],
          subTotal: 10000,
          total: 10000,
          itemCount: 1,
        }),
      })
    })

    // Mock promo code
    await page.route('**/api/cart/promo', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isValid: true,
          message: 'Промокод применён! Скидка: 1 000 ₽',
          discountAmount: 1000,
          newTotal: 9000,
        }),
      })
    })

    await page.goto('/cart')

    // Enter promo code
    await page.locator('input[placeholder="Промокод"]').fill('TEST10')
    await page.locator('text=Применить').click()

    // Should show success toast
    await expect(page.locator('text=Промокод применён')).toBeVisible()
  })
})
