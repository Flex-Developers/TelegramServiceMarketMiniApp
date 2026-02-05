using Microsoft.AspNetCore.SignalR;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Application.Services;
using TelegramMarketplace.WebAPI.Hubs;

namespace TelegramMarketplace.WebAPI.Services;

public class SignalRNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationAsync(Guid userId, NotificationDto notification)
    {
        try
        {
            await _hubContext.Clients
                .Group($"user:{userId}")
                .SendAsync("ReceiveNotification", notification);

            _logger.LogDebug("Sent notification to user {UserId}: {Title}", userId, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
        }
    }

    public async Task SendOrderUpdateAsync(Guid userId, OrderDto order)
    {
        try
        {
            // Send to user
            await _hubContext.Clients
                .Group($"user:{userId}")
                .SendAsync("OrderUpdated", order);

            // Send to anyone subscribed to this specific order
            await _hubContext.Clients
                .Group($"order:{order.Id}")
                .SendAsync("OrderUpdated", order);

            _logger.LogDebug("Sent order update to user {UserId}: Order {OrderId}", userId, order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order update to user {UserId}", userId);
        }
    }

    public async Task SendPaymentUpdateAsync(Guid userId, PaymentDto payment)
    {
        try
        {
            await _hubContext.Clients
                .Group($"user:{userId}")
                .SendAsync("PaymentUpdated", payment);

            _logger.LogDebug("Sent payment update to user {UserId}: Payment {PaymentId}", userId, payment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment update to user {UserId}", userId);
        }
    }
}
