using MassTransit;
using Shared.Contracts.Orders;

namespace Payment.Gateway.Api.Services;

public sealed class PaymentEventPublisher(
    IPublishEndpoint publishEndpoint,
    ILogger<PaymentEventPublisher> logger) : IPaymentEventPublisher
{
    public async Task PublishAsync(
        PaymentConfirmedEvent paymentConfirmedEvent,
        CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(paymentConfirmedEvent, cancellationToken);

        logger.LogInformation(
            "Published PaymentConfirmedEvent for OrderId {OrderId}",
            paymentConfirmedEvent.OrderId);
    }
}