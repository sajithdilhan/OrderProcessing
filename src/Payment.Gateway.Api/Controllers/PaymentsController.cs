using Microsoft.AspNetCore.Mvc;
using Payment.Gateway.Api.Services;
using Shared.Contracts.Orders;

namespace Payment.Gateway.Api.Controllers;

[ApiController]
[Route("")]
public class PaymentsController(IPaymentEventPublisher paymentEventPublisher, ILogger<PaymentsController> logger) : ControllerBase
{
    [HttpPost("payment-confirmed")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PaymentConfirmed([FromQuery] PaymentConfirmedEvent paymentConfirmedEvent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(paymentConfirmedEvent.OrderId))
        {
            return BadRequest("OrderId is required.");
        }

        if (string.IsNullOrWhiteSpace(paymentConfirmedEvent.CustomerId))
        {
            return BadRequest("CustomerId is required.");
        }

        if (paymentConfirmedEvent.Items is null || paymentConfirmedEvent.Items.Length == 0)
        {
            return BadRequest("At least one item is required.");
        }

        logger.LogInformation(
            "Received payment confirmation for OrderId {OrderId} and CustomerId {CustomerId}. CorrelationId: {CorrelationId}",
            paymentConfirmedEvent.OrderId,
            paymentConfirmedEvent.CustomerId,
            paymentConfirmedEvent.CorrelationId);

        await paymentEventPublisher.PublishAsync(paymentConfirmedEvent, cancellationToken);

        logger.LogInformation("Published payment confirmation event for OrderId {OrderId} with CorrelationId {CorrelationId}", paymentConfirmedEvent.OrderId, paymentConfirmedEvent.CorrelationId);
        return Accepted();
    }
}