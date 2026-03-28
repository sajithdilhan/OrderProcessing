using Order.Orchestrator.Api.Application.Interfaces;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Api.Application.Services;

public sealed class PendingOrderSyncService(
    IOmsClient omsClient,
    IInventoryClient inventoryClient,
    IProcessedOrderStore processedOrderStore,
    ILogger<PendingOrderSyncService> logger)
{
    public async Task SyncPendingOrdersAsync(string correlationId, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Starting pending order sync for CorrelationId {CorrelationId}",
                correlationId);

            var orders = await omsClient.GetPendingOrdersAsync(cancellationToken);

            logger.LogInformation(
                "Fetched {OrderCount} pending orders for CorrelationId {CorrelationId}",
                orders.Count,
                correlationId);

            foreach (var order in orders)
            {
                var key = $"allocate:{order.OrderId}";

                if (await processedOrderStore.ExistsAsync(key, cancellationToken))
                {
                    logger.LogInformation(
                        "Skipping already processed allocation for OrderId {OrderId}",
                        order.OrderId);

                    continue;
                }

                var request = new InventoryAllocationRequest(order.OrderId, order.Items);

                await inventoryClient.AllocateAsync(request, cancellationToken);
                await processedOrderStore.MarkProcessedAsync(key, cancellationToken);

                logger.LogInformation(
                    "Allocated inventory for OrderId {OrderId} with CorrelationId {CorrelationId}",
                    order.OrderId,
                    correlationId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Pending order sync failed for CorrelationId {CorrelationId}",
                correlationId);
        }
    }
}