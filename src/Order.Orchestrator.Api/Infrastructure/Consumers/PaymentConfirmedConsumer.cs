using MassTransit;
using Order.Orchestrator.Api.Application.Services;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Api.Consumers;

public sealed class PaymentConfirmedConsumer(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<PaymentConfirmedConsumer> logger) : IConsumer<PaymentConfirmedEvent>
{
    public async Task Consume(ConsumeContext<PaymentConfirmedEvent> context)
    {
        logger.LogInformation(
            "Consumed PaymentConfirmedEvent for OrderId {OrderId}",
            context.Message.OrderId);

        using var scope = serviceScopeFactory.CreateScope();

        var processor = scope.ServiceProvider.GetRequiredService<PaymentConfirmedProcessor>();

        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}