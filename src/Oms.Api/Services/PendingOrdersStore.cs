using Shared.Contracts.Orders;

namespace Oms.Api.Services;

public sealed class PendingOrdersStore
{
    private readonly List<PendingOrder> _orders =
    [
        new(
            OrderId: "ORD-1001",
            CustomerId: "CUST-001",
            Items: ["ITEM-A", "ITEM-B"],
            Total: 149.90m),

        new(
            OrderId: "ORD-1002",
            CustomerId: "CUST-002",
            Items: ["ITEM-C"],
            Total: 59.90m),

        new(
            OrderId: "ORD-1003",
            CustomerId: "CUST-003",
            Items: ["ITEM-D", "ITEM-E", "ITEM-F"],
            Total: 249.00m)
    ];

    public IReadOnlyList<PendingOrder> GetPendingOrders()
    {
        return _orders.AsReadOnly();
    }
}

