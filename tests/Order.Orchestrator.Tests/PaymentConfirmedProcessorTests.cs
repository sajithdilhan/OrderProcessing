using Microsoft.Extensions.Logging.Abstractions;
using Order.Orchestrator.Api.Application.Services;
using Order.Orchestrator.Api.Infrastructure.Storage;
using Order.Orchestrator.Tests.TestDoubles;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Tests.Unit;

public sealed class PaymentConfirmedProcessorTests
{
    [Fact]
    public async Task QueueProcessor_ProcessesOrder_Success()
    {
        var inventoryClient = new FakeInventoryClient();
        var processedStore = new InMemoryProcessedOrderStore();
        var deadLetterStore = new InMemoryDeadLetterStore();

        var sut = new PaymentConfirmedProcessor(
            inventoryClient,
            processedStore,
            deadLetterStore,
            NullLogger<PaymentConfirmedProcessor>.Instance);

        var message = new PaymentConfirmedEvent(
            "ORD-1001",
            "CUST-001",
            ["ITEM-A", "ITEM-B"],
            149.90m,
            DateTime.UtcNow);

        await sut.ProcessAsync(message, CancellationToken.None);

        Assert.Single(inventoryClient.Reserved);

        var deadLetters = await deadLetterStore.GetAllAsync(CancellationToken.None);
        Assert.Empty(deadLetters);

        var exists = await processedStore.ExistsAsync("reserve:ORD-1001", CancellationToken.None);
        Assert.True(exists);
    }

    [Fact]
    public async Task QueueProcessor_FinalFailure_DeadLetters()
    {
        var inventoryClient = new FakeInventoryClient
        {
            ThrowOnReserve = true
        };

        var processedStore = new InMemoryProcessedOrderStore();
        var deadLetterStore = new InMemoryDeadLetterStore();

        var sut = new PaymentConfirmedProcessor(
            inventoryClient,
            processedStore,
            deadLetterStore,
            NullLogger<PaymentConfirmedProcessor>.Instance);

        var message = new PaymentConfirmedEvent(
            "ORD-1002",
            "CUST-002",
            ["ITEM-X"],
            59.90m,
            DateTime.UtcNow);

        await sut.ProcessAsync(message, CancellationToken.None);

        Assert.Empty(inventoryClient.Reserved);

        var deadLetters = await deadLetterStore.GetAllAsync(CancellationToken.None);
        var deadLetter = Assert.Single(deadLetters);

        Assert.Equal("ORD-1002", deadLetter.OriginalMessage.OrderId);

        var exists = await processedStore.ExistsAsync("reserve:ORD-1002", CancellationToken.None);
        Assert.False(exists);
    }
}