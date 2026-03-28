using Shared.Contracts.Orders;

namespace Order.Orchestrator.Api.Application.Interfaces;

public interface IInventoryClient
{
    Task AllocateAsync(InventoryAllocationRequest request, CancellationToken cancellationToken);
    Task ReserveAsync(InventoryAllocationRequest request, CancellationToken cancellationToken);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);
}