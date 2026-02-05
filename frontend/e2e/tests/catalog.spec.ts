import { test, expect } from '@playwright/test'

test.describe('Catalog Page', () => {
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
          initData: '',
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
          openTelegramLink: () => {},
          close: () => {},
          showAlert: () => {},
          showConfirm: () => {},
          showPopup: () => {},
          CloudStorage: {
            setItem: () => {},
            getItem: () => {},
            removeItem: () => {},
          },
        },
      }
    })
  })

  test('should display catalog page', async ({ page }) => {
    await page.goto('/catalog')

    // Check page title
    await expect(page.locator('h1')).toContainText('Каталог')

    // Check search input exists
    await expect(page.locator('input[placeholder*="Поиск"]')).toBeVisible()
  })

  test('should filter services by search term', async ({ page }) => {
    await page.goto('/catalog')

    // Type in search
    const searchInput = page.locator('input[placeholder*="Поиск"]')
    await searchInput.fill('дизайн')

    // Wait for results to update
    await page.waitForTimeout(500)

    // Verify URL has search param
    await expect(page).toHaveURL(/q=дизайн/)
  })

  test('should show filter panel', async ({ page }) => {
    await page.goto('/catalog')

    // Click filter button
    await page.locator('button:has(svg)').last().click()

    // Check filter options appear
    await expect(page.locator('text=Сортировка')).toBeVisible()
    await expect(page.locator('text=Цена от')).toBeVisible()
    await expect(page.locator('text=Минимальный рейтинг')).toBeVisible()
  })

  test('should navigate to service detail', async ({ page }) => {
    await page.goto('/catalog')

    // Mock API response
    await page.route('**/api/services*', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'test-service-1',
              title: 'Тестовая услуга',
              price: 1000,
              priceType: 'Fixed',
              deliveryDays: 3,
              averageRating: 4.5,
              reviewCount: 10,
              thumbnailUrl: null,
              seller: {
                id: 'seller-1',
                firstName: 'Продавец',
                isVerified: true,
                averageRating: 4.5,
              },
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
          totalPages: 1,
          hasNextPage: false,
          hasPreviousPage: false,
        }),
      })
    })

    await page.goto('/catalog')

    // Click on service card
    await page.locator('a[href*="/service/"]').first().click()

    // Verify navigation
    await expect(page).toHaveURL(/\/service\//)
  })
})
