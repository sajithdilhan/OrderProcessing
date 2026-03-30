using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Order.Orchestrator.Api.Application.Interfaces;
using Order.Orchestrator.Api.Application.Services;
using Order.Orchestrator.Api.Consumers;
using Order.Orchestrator.Api.Infrastructure.Storage;
using Order.Orchestrator.Tests.Fakes;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Tests.Integration;

public sealed class QueueStressTests
{
    [Fact]
    public async Task Queue_StressTest_100MessagesProcessedWithinTargetTime()
    {
        var inventoryClient = new FakeInventoryClient();
        var startedAt = DateTime.UtcNow;

        await using var provider = new ServiceCollection()
            .AddSingleton<IInventoryClient>(inventoryClient)
            .AddSingleton<IProcessedOrderStore, InMemoryProcessedOrderStore>()
            .AddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>()
            .AddSingleton<ILogger<PaymentConfirmedProcessor>>(NullLogger<PaymentConfirmedProcessor>.Instance)
            .AddSingleton<PaymentConfirmedProcessor>()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<PaymentConfirmedConsumer>();
            })
            .AddSingleton<ILogger<PaymentConfirmedConsumer>>(NullLogger<PaymentConfirmedConsumer>.Instance)
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var tasks = Enumerable.Range(1, 100)
                .Select(i => harness.Bus.Publish(new PaymentConfirmedEvent(
                    $"ORD-{i:0000}",
                    $"CUST-{i:0000}",
                    [$"ITEM-{i:0000}"],
                    10m + i,
                    DateTime.UtcNow)));

            await Task.WhenAll(tasks);

            await TestWait.UntilAsync(
                () => inventoryClient.Reserved.Count == 100,
                timeout: TimeSpan.FromSeconds(10));

            var elapsed = DateTime.UtcNow - startedAt;

            Assert.Equal(100, inventoryClient.Reserved.Count);
            Assert.True(elapsed < TimeSpan.FromSeconds(10),
                $"Expected 100 messages within 10s, actual: {elapsed.TotalSeconds:n2}s");
        }
        finally
        {
            await harness.Stop();
        }
    }
}