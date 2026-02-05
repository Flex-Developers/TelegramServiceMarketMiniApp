using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramMarketplace.Application.DTOs;
using TelegramMarketplace.Application.Services;
using TelegramMarketplace.Domain.Enums;

namespace TelegramMarketplace.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Create YooKassa payment
    /// </summary>
    [HttpPost("yookassa/create")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateYooKassaPayment([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await _paymentService.CreatePaymentAsync(request.OrderId, PaymentProvider.YooKassa, request.ReturnUrl, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>
    /// Create Robokassa payment
    /// </summary>
    [HttpPost("robokassa/create")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRobokassaPayment([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await _paymentService.CreatePaymentAsync(request.OrderId, PaymentProvider.Robokassa, request.ReturnUrl, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>
    /// Create Telegram Stars payment
    /// </summary>
    [HttpPost("telegram/create")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTelegramStarsPayment([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await _paymentService.CreatePaymentAsync(request.OrderId, PaymentProvider.TelegramStars, null, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>
    /// YooKassa webhook handler
    /// </summary>
    [HttpPost("webhook/yookassa")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> YooKassaWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        _logger.LogInformation("Received YooKassa webhook: {Body}", body);

        try
        {
            var webhook = JsonSerializer.Deserialize<YooKassaWebhookDto>(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (webhook == null)
                return BadRequest();

            var result = await _paymentService.ProcessYooKassaWebhookAsync(webhook, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogError("YooKassa webhook processing failed: {Error}", result.Error);
                return BadRequest();
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing YooKassa webhook");
            return BadRequest();
        }
    }

    /// <summary>
    /// Robokassa result callback handler
    /// </summary>
    [HttpPost("webhook/robokassa")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RobokassaCallback([FromForm] RobokassaCallbackDto callback, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received Robokassa callback: InvId={InvId}, OutSum={OutSum}",
            callback.InvId, callback.OutSum);

        var result = await _paymentService.ProcessRobokassaCallbackAsync(callback, cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogError("Robokassa callback processing failed: {Error}", result.Error);
            return BadRequest(result.Error);
        }

        // Robokassa expects "OK{InvId}" response
        return Content($"OK{callback.InvId}");
    }

    /// <summary>
    /// Telegram Stars payment confirmation
    /// </summary>
    [HttpPost("telegram/confirm")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmTelegramStarsPayment([FromBody] TelegramStarsPaymentDto payment, CancellationToken cancellationToken)
    {
        var result = await _paymentService.ProcessTelegramStarsPaymentAsync(payment, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = "Payment confirmed" });
    }

    /// <summary>
    /// Get payment status
    /// </summary>
    [HttpGet("{id:guid}/status")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetPaymentStatusAsync(id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Request refund
    /// </summary>
    [HttpPost("{id:guid}/refund")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefundPayment(Guid id, CancellationToken cancellationToken)
    {
        var result = await _paymentService.RefundPaymentAsync(id, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(new { message = "Refund processed" });
    }
}
