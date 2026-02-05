# Payment Integration Guide

This guide covers integration with all three payment providers: YooKassa, Robokassa, and Telegram Stars.

## YooKassa (ЮKassa)

### Setup

1. Register at [yookassa.ru](https://yookassa.ru)
2. Create a shop and get credentials:
   - Shop ID
   - Secret Key
3. Configure webhook URL in YooKassa dashboard

### Environment Variables

```env
YOOKASSA_SHOP_ID=your_shop_id
YOOKASSA_SECRET_KEY=your_secret_key
```

### Webhook Configuration

1. Go to YooKassa Dashboard → Integration → HTTP-notifications
2. Add webhook URL: `https://your-domain.com/api/payments/webhook/yookassa`
3. Select events: `payment.succeeded`, `payment.canceled`, `refund.succeeded`

### Testing

Use test credentials in sandbox mode:
- Test cards: [YooKassa Test Cards](https://yookassa.ru/developers/payment-acceptance/testing-and-going-live/test-cards)

### Payment Flow

```
1. User clicks "Pay" → Frontend calls POST /api/payments/yookassa/create
2. Backend creates payment via YooKassa API
3. Backend returns confirmation URL
4. Frontend redirects user to YooKassa payment page
5. User completes payment
6. YooKassa sends webhook to backend
7. Backend updates order status
8. User is redirected to success page
```

---

## Robokassa

### Setup

1. Register at [robokassa.ru](https://robokassa.ru)
2. Get credentials:
   - Merchant Login
   - Password 1 (for payment generation)
   - Password 2 (for result verification)

### Environment Variables

```env
ROBOKASSA_LOGIN=your_merchant_login
ROBOKASSA_PASSWORD1=your_password_1
ROBOKASSA_PASSWORD2=your_password_2
```

### URL Configuration

In Robokassa dashboard, set:
- **Result URL**: `https://your-domain.com/api/payments/webhook/robokassa`
- **Success URL**: `https://your-domain.com/payment/success`
- **Fail URL**: `https://your-domain.com/payment/failed`

### Signature Calculation

```
Payment creation: MD5(MerchantLogin:OutSum:InvId:Password1[:Shp_xxx])
Result verification: MD5(OutSum:InvId:Password2[:Shp_xxx])
```

### Testing

Enable test mode in environment:
```env
# When IsTestMode=true, payments go to sandbox
```

---

## Telegram Stars

### Setup

1. Contact [@BotSupport](https://t.me/BotSupport) to enable payments
2. No additional credentials needed (uses bot token)

### How It Works

1. Backend creates an invoice link via Telegram Bot API
2. User opens the invoice link in Telegram
3. User pays with Telegram Stars
4. Telegram sends `successful_payment` update
5. Backend confirms the payment

### Currency

- Telegram Stars use currency code `XTR`
- 1 Star ≈ 0.013 USD (rate varies)
- Convert your prices: `stars = ceil(price_rub / 1.5)`

### Refunds

```csharp
// Use refundStarPayment API
await _telegramClient.RefundStarPaymentAsync(userId, telegramPaymentChargeId);
```

---

## Testing Payments

### Mock Payment Gateway

For development, you can mock payment responses:

```csharp
// In development, bypass actual payment providers
if (Environment.IsDevelopment())
{
    payment.MarkAsCompleted();
    await _orderService.MarkAsPaidAsync(orderId);
    return new PaymentResultDto(...);
}
```

### Test Cards (YooKassa)

| Card Number | Result |
|-------------|--------|
| 5555 5555 5555 4444 | Success |
| 5555 5555 5555 4477 | Declined |
| 5555 5555 5555 4592 | 3D Secure |

### Webhook Testing

Use tools like [webhook.site](https://webhook.site) or ngrok to test webhooks locally:

```bash
# Start ngrok
ngrok http 5000

# Update webhook URLs in payment dashboards
```

---

## Error Handling

### Common Errors

| Error Code | Description | Action |
|------------|-------------|--------|
| `PAYMENT_ERROR` | Provider API error | Retry or show error |
| `INVALID_SIGNATURE` | Webhook signature mismatch | Log and investigate |
| `ORDER_NOT_FOUND` | Order doesn't exist | Log security event |
| `ALREADY_PAID` | Duplicate payment attempt | Ignore, return success |

### Retry Logic

Implement exponential backoff for failed payment checks:

```typescript
const checkPaymentStatus = async (paymentId: string, retries = 3) => {
  for (let i = 0; i < retries; i++) {
    const status = await paymentsApi.getStatus(paymentId);
    if (status.status !== 'Pending') return status;
    await sleep(Math.pow(2, i) * 1000);
  }
  return { status: 'Pending' };
};
```

---

## Security Considerations

1. **Never store payment credentials in frontend code**
2. **Always validate webhook signatures**
3. **Use idempotency keys for payment creation**
4. **Log all payment events for audit**
5. **Implement rate limiting on payment endpoints**
6. **Use HTTPS for all payment-related communication**

---

## Monitoring

Track these metrics:
- Payment success rate
- Average payment time
- Refund rate
- Provider availability
- Webhook processing time

Set up alerts for:
- Payment failures > 5% in 1 hour
- Webhook processing failures
- Provider API errors
