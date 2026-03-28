using Shared.Contracts.Orders;

namespace Payment.Gateway.Api.Services;

public sealed class PaymentEventPublisher(
    ILogger<PaymentEventPublisher> logger) : IPaymentEventPublisher
{
    public Task PublishAsync(
        PaymentConfirmedEvent paymentConfirmedEvent,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Simulated publish of payment confirmed event for OrderId {OrderId} at {PaidAt}",
            paymentConfirmedEvent.OrderId,
            paymentConfirmedEvent.PaidAt);

        return Task.CompletedTask;
    }
}