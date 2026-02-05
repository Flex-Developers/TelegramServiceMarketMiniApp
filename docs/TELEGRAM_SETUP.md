# Telegram Bot & Mini App Setup

## 1. Create a Telegram Bot

1. Open [@BotFather](https://t.me/BotFather) in Telegram
2. Send `/newbot` command
3. Follow the prompts to set a name and username
4. Save the **bot token** (format: `123456789:ABCdefGHIjklMNOpqrsTUVwxyz`)

## 2. Configure the Mini App

1. In BotFather, send `/mybots`
2. Select your bot
3. Go to **Bot Settings** → **Configure Mini App**
4. Enable Mini App
5. Set the **Web App URL** (must be HTTPS in production)
6. Optionally customize:
   - App icon
   - Loading placeholder colors (light/dark theme)

## 3. Set Up Bot Menu Button

1. In BotFather, send `/mybots`
2. Select your bot → **Bot Settings** → **Menu Button**
3. Choose **Configure menu button**
4. Set the button text (e.g., "Открыть маркетплейс")
5. Set the Web App URL

## 4. Configure Bot Commands

Send to BotFather:
```
/setcommands
```

Then provide commands:
```
start - Открыть маркетплейс
help - Помощь
catalog - Каталог услуг
orders - Мои заказы
profile - Мой профиль
```

## 5. Environment Variables

Add to your environment:

```env
TELEGRAM_BOT_TOKEN=your_bot_token_here
TELEGRAM_BOT_USERNAME=your_bot_username
WEBAPP_URL=https://your-domain.com
```

## 6. Testing the Mini App

### Local Development

For local testing, you can use ngrok or similar:

```bash
# Start ngrok
ngrok http 5173

# Use the HTTPS URL from ngrok as your WEBAPP_URL
```

### Test in Telegram

1. Open your bot in Telegram
2. Click the menu button or type `/start`
3. The Mini App should load within Telegram

## 7. Authentication Flow

The Mini App authenticates users via `initData`:

1. Telegram sends `initData` when the app loads
2. Backend validates the `initData` signature using bot token
3. If valid, a JWT is issued for subsequent API calls

### Validating initData (Backend)

```csharp
// The signature is validated in TelegramAuthService
// 1. Parse the initData query string
// 2. Remove 'hash' parameter
// 3. Sort remaining parameters alphabetically
// 4. Join with newlines to create data-check-string
// 5. Calculate HMAC-SHA256(data-check-string, HMAC-SHA256(bot_token, "WebAppData"))
// 6. Compare with provided hash
```

## 8. Webhook Configuration (Optional)

For bot commands and inline queries:

1. Set webhook URL:
   ```bash
   curl -X POST "https://api.telegram.org/bot<TOKEN>/setWebhook" \
     -d "url=https://your-domain.com/api/telegram/webhook"
   ```

2. Verify webhook:
   ```bash
   curl "https://api.telegram.org/bot<TOKEN>/getWebhookInfo"
   ```

## 9. Telegram Stars Integration

For Telegram Stars payments:

1. Contact [@BotSupport](https://t.me/BotSupport) to enable payments
2. Set up payment provider in BotFather
3. Use the `createInvoiceLink` API to generate payment links

## 10. Best Practices

### UI/UX
- Follow [Telegram Design Guidelines](https://core.telegram.org/bots/webapps#design-guidelines)
- Support both light and dark themes
- Use native Telegram colors via CSS variables
- Implement haptic feedback for interactions

### Security
- Always validate `initData` on backend
- Check `auth_date` is not older than 1 hour
- Use HTTPS in production
- Don't store sensitive data in `CloudStorage`

### Performance
- Keep initial bundle < 200KB
- Use lazy loading for routes
- Implement skeleton loading states
- Cache API responses appropriately

## Useful Links

- [Telegram Mini Apps Documentation](https://core.telegram.org/bots/webapps)
- [Telegram Bot API](https://core.telegram.org/bots/api)
- [@twa-dev/sdk Documentation](https://github.com/twa-dev/sdk)
- [Telegram Design Guidelines](https://core.telegram.org/bots/webapps#design-guidelines)
