using Order.Orchestrator.Api.Application.Interfaces;
using Order.Orchestrator.Api.Application.Models;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Api.Application.Services;

public sealed class PaymentConfirmedProcessor(
    IInventoryClient inventoryClient,
    IProcessedOrderStore processedOrderStore,
    IDeadLetterStore deadLetterStore,
    ILogger<PaymentConfirmedProcessor> logger)
{
    public async Task ProcessAsync(PaymentConfirmedEvent message, CancellationToken cancellationToken)
    {
        var key = $"reserve:{message.OrderId}";

        if (await processedOrderStore.ExistsAsync(key, cancellationToken))
        {
            logger.LogInformation("Skipping already processed reservation for OrderId {OrderId}", message.OrderId);

            return;
        }

        try
        {
            logger.LogInformation("Processing payment event for OrderId {OrderId} with CorrelationId {CorrelationId}", message.OrderId, message.CorrelationId);

            var request = new InventoryAllocationRequest(message.OrderId, message.Items, message.CorrelationId);

            await inventoryClient.ReserveAsync(request, cancellationToken);
            await processedOrderStore.MarkProcessedAsync(key, cancellationToken);

            logger.LogInformation("Reserved inventory for OrderId {OrderId} with CorrelationId {CorrelationId}", message.OrderId, message.CorrelationId);
        }
        catch (Exception ex)
        {
            var deadLetter = new DeadLetterMessage(
                OriginalMessage: message,
                RetryCount: 1,
                FailureReason: ex.Message,
                FailedAtUtc: DateTime.UtcNow);

            await deadLetterStore.AddAsync(deadLetter, cancellationToken);

            logger.LogError(ex,"Dead-lettered message for OrderId {OrderId} with CorrelationId {CorrelationId}", message.OrderId, message.CorrelationId);
        }
    }
}