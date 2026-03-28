namespace Inventory.Service.Api.Models;

public sealed record InventoryOperationResponse(
    bool Success,
    string Operation,
    string OrderId,
    string Message);