namespace Shared.Contracts.Orders;

// Flow 1

public record PendingPingRequest(string CorrelationId);

public record PendingOrder(string OrderId, string CustomerId, string[] Items, decimal Total);

// Flow 2

public record PaymentConfirmedEvent(string OrderId, string CustomerId, string[] Items, decimal Total, DateTime PaidAt);

public record InventoryAllocationRequest(string OrderId, string[] Items);
