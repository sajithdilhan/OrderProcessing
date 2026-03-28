using Shared.Contracts.Orders;

namespace Payment.Gateway.Api.Services;

public interface IPaymentEventPublisher
{
    Task PublishAsync(PaymentConfirmedEvent paymentConfirmedEvent, CancellationToken cancellationToken);
}