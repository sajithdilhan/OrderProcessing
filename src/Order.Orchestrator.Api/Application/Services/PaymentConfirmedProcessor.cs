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
            logger.LogInformation(
                "Skipping already processed reservation for OrderId {OrderId}",
                message.OrderId);

            return;
        }

        const int maxAttempts = 3;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                logger.LogInformation(
                    "Processing payment confirmed event for OrderId {OrderId}, attempt {Attempt}",
                    message.OrderId,
                    attempt);

                var request = new InventoryAllocationRequest(message.OrderId, message.Items);

                await inventoryClient.ReserveAsync(request, cancellationToken);
                await processedOrderStore.MarkProcessedAsync(key, cancellationToken);

                logger.LogInformation(
                    "Reserved inventory for OrderId {OrderId}",
                    message.OrderId);

                return;
            }
            catch (Exception ex)
            {
                lastException = ex;

                logger.LogWarning(
                    ex,
                    "Retry attempt {Attempt} failed for OrderId {OrderId}",
                    attempt,
                    message.OrderId);

                if (attempt < maxAttempts)
                {
                    var delay = attempt switch
                    {
                        1 => TimeSpan.FromSeconds(2),
                        2 => TimeSpan.FromSeconds(4),
                        _ => TimeSpan.FromSeconds(8)
                    };

                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        var deadLetter = new DeadLetterMessage(
            OriginalMessage: message,
            RetryCount: maxAttempts,
            FailureReason: lastException?.Message ?? "Unknown failure",
            FailedAtUtc: DateTime.UtcNow);

        await deadLetterStore.AddAsync(deadLetter, cancellationToken);

        logger.LogError(
            lastException,
            "Dead-lettered message for OrderId {OrderId} after {RetryCount} attempts",
            message.OrderId,
            maxAttempts);
    }
}