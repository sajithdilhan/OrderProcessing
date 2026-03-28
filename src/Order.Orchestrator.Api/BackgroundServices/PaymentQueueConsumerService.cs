using Order.Orchestrator.Api.Application.Interfaces;
using Order.Orchestrator.Api.Application.Services;

namespace Order.Orchestrator.Api.BackgroundServices;

public sealed class PaymentQueueConsumerService(
    IOrderQueue orderQueue,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<PaymentQueueConsumerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Payment queue consumer started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await orderQueue.DequeueAsync(stoppingToken);

                logger.LogInformation(
                    "Dequeued payment confirmed event for OrderId {OrderId}",
                    message.OrderId);

                using var scope = serviceScopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<PaymentConfirmedProcessor>();

                await processor.ProcessAsync(message, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Payment queue consumer is stopping");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in payment queue consumer");
            }
        }
    }
}