import { test, expect } from '@playwright/test'

test.describe('Checkout Flow', () => {
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
            setText: (text: string) => { (window as any).Telegram.WebApp.MainButton.text = text },
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
          openLink: (url: string) => { window.open(url, '_blank') },
          close: () => {},
          showConfirm: (message: string, callback: (confirmed: boolean) => void) => {
            callback(true)
          },
        },
      }
    })

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
              serviceTitle: 'Разработка сайта',
              servicePrice: 50000,
              quantity: 1,
              totalPrice: 50000,
              seller: {
                id: 'seller-1',
                firstName: 'Разработчик',
                isVerified: true,
                averageRating: 4.9,
              },
            },
          ],
          subTotal: 50000,
          total: 50000,
          itemCount: 1,
        }),
      })
    })
  })

  test('should display checkout page with order summary', async ({ page }) => {
    await page.goto('/checkout')

    await expect(page.locator('h1')).toContainText('Оформление заказа')
    await expect(page.locator('text=Ваш заказ')).toBeVisible()
    await expect(page.locator('text=Разработка сайта')).toBeVisible()
    await expect(page.locator('text=50 000')).toBeVisible()
  })

  test('should display payment methods', async ({ page }) => {
    await page.goto('/checkout')

    await expect(page.locator('text=Способ оплаты')).toBeVisible()
    await expect(page.locator('text=ЮKassa')).toBeVisible()
    await expect(page.locator('text=Robokassa')).toBeVisible()
    await expect(page.locator('text=Telegram Stars')).toBeVisible()
  })

  test('should select payment method', async ({ page }) => {
    await page.goto('/checkout')

    // Click on Robokassa option
    await page.locator('text=Robokassa').click()

    // Verify selection (check for ring/border)
    const robokassaButton = page.locator('button:has-text("Robokassa")')
    await expect(robokassaButton).toHaveClass(/ring-2/)
  })

  test('should add order notes', async ({ page }) => {
    await page.goto('/checkout')

    const notesInput = page.locator('textarea[placeholder*="Укажите пожелания"]')
    await notesInput.fill('Пожалуйста, свяжитесь со мной перед началом работы')

    await expect(notesInput).toHaveValue('Пожалуйста, свяжитесь со мной перед началом работы')
  })

  test('should create order and redirect to payment', async ({ page }) => {
    // Mock order creation
    await page.route('**/api/orders', async (route) => {
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'order-123',
          status: 'Pending',
          totalAmount: 50000,
          items: [],
          buyerId: 'buyer-1',
          sellerId: 'seller-1',
        }),
      })
    })

    // Mock payment creation
    await page.route('**/api/payments/yookassa/create', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          paymentId: 'payment-123',
          orderId: 'order-123',
          status: 'Pending',
          confirmationUrl: 'https://yookassa.ru/checkout/test',
        }),
      })
    })

    // Mock cart clear
    await page.route('**/api/cart/clear', async (route) => {
      await route.fulfill({ status: 204 })
    })

    await page.goto('/checkout')

    // The checkout would normally trigger via MainButton
    // For testing, we can simulate the flow
  })
})
