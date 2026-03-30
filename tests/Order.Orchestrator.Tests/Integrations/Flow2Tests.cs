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

public sealed class Flow2Tests
{
    [Fact]
    public async Task Flow2_PaymentConfirmed_InventoryReceives()
    {
        var inventoryClient = new FakeInventoryClient();

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
            var message = new PaymentConfirmedEvent(
                "ORD-3001",
                "CUST-301",
                ["SKU-1", "SKU-2"],
                199m,
                DateTime.UtcNow);

            await harness.Bus.Publish(message);

            Assert.True(await harness.Consumed.Any<PaymentConfirmedEvent>());

            await TestWait.UntilAsync(
                () => inventoryClient.Reserved.Count == 1,
                timeout: TimeSpan.FromSeconds(5));

            Assert.Single(inventoryClient.Reserved);
        }
        finally
        {
            await harness.Stop();
        }
    }
}