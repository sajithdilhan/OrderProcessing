using Shared.Contracts.Orders;

namespace Order.Orchestrator.Api.Application.Interfaces;

public interface IOrderQueue
{
    ValueTask EnqueueAsync(PaymentConfirmedEvent message, CancellationToken cancellationToken);
    ValueTask<PaymentConfirmedEvent> DequeueAsync(CancellationToken cancellationToken);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);
}