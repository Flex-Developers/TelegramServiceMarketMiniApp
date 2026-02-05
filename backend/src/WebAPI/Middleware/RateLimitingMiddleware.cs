using System.Collections.Concurrent;
using System.Security.Claims;

namespace TelegramMarketplace.WebAPI.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimits = new();

    // Configuration
    private const int RequestsPerMinute = 100;
    private const int RequestsPerMinuteAnonymous = 30;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for webhooks
        if (context.Request.Path.StartsWithSegments("/api/payments/webhook") ||
            context.Request.Path.StartsWithSegments("/api/telegram/webhook"))
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = !string.IsNullOrEmpty(userId) ? $"user:{userId}" : $"ip:{clientIp}";
        var limit = !string.IsNullOrEmpty(userId) ? RequestsPerMinute : RequestsPerMinuteAnonymous;

        var rateLimitInfo = _rateLimits.GetOrAdd(key, _ => new RateLimitInfo());

        lock (rateLimitInfo)
        {
            // Reset counter if window expired
            if (DateTime.UtcNow - rateLimitInfo.WindowStart > TimeSpan.FromMinutes(1))
            {
                rateLimitInfo.WindowStart = DateTime.UtcNow;
                rateLimitInfo.RequestCount = 0;
            }

            rateLimitInfo.RequestCount++;

            if (rateLimitInfo.RequestCount > limit)
            {
                _logger.LogWarning("Rate limit exceeded for {Key}", key);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = "60";
                context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = "0";

                return;
            }

            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = (limit - rateLimitInfo.RequestCount).ToString();
        }

        await _next(context);
    }

    private class RateLimitInfo
    {
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
        public int RequestCount { get; set; }
    }
}
