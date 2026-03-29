using Shared.Contracts.Orders;

namespace Inventory.Service.Api.Services;

public sealed class InventoryOperationsStore
{
    private readonly List<InventoryAllocationRequest> _allocatedRequests = [];
    private readonly List<InventoryAllocationRequest> _reservedRequests = [];
    private readonly object _lock = new();

    public void AddAllocated(InventoryAllocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_lock)
        {
            _allocatedRequests.Add(request);
        }
    }

    public void AddReserved(InventoryAllocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_lock)
        {
            _reservedRequests.Add(request);
        }
    }
}