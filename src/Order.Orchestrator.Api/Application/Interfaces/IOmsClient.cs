using Shared.Contracts.Orders;

namespace Order.Orchestrator.Api.Application.Interfaces;

public interface IOmsClient
{
    Task<IReadOnlyList<PendingOrder>> GetPendingOrdersAsync(CancellationToken cancellationToken, string? correlationId);
}