using Microsoft.Extensions.Logging.Abstractions;
using Order.Orchestrator.Api.Application.Services;
using Order.Orchestrator.Api.Infrastructure.Storage;
using Order.Orchestrator.Tests.Fakes;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Tests.Unit;

public sealed class PaymentConfirmedProcessorTests
{
    private static PaymentConfirmedProcessor BuildProcessor(
        FakeInventoryClient? inventoryClient = null,
        InMemoryProcessedOrderStore? processedStore = null,
        InMemoryDeadLetterStore? deadLetterStore = null) =>
        new(
            inventoryClient ?? new FakeInventoryClient(),
            processedStore ?? new InMemoryProcessedOrderStore(),
            deadLetterStore ?? new InMemoryDeadLetterStore(),
            NullLogger<PaymentConfirmedProcessor>.Instance);

    private static PaymentConfirmedEvent MakeEvent(string orderId = "ORD-001") =>
        new(orderId, "CUST-001", ["ITEM-A"], 99.99m, DateTime.UtcNow, "CORR-001");

    [Fact]
    public async Task QueueProcessor_ProcessesOrder_Success()
    {
        var inventoryClient = new FakeInventoryClient();
        var processedStore = new InMemoryProcessedOrderStore();
        var deadLetterStore = new InMemoryDeadLetterStore();
        var sut = BuildProcessor(inventoryClient, processedStore, deadLetterStore);

        await sut.ProcessAsync(new PaymentConfirmedEvent("ORD-1001", "CUST-001", ["ITEM-A", "ITEM-B"], 149.90m, DateTime.UtcNow, "CORR-1001"), CancellationToken.None);

        Assert.Single(inventoryClient.Reserved);
        Assert.Empty(await deadLetterStore.GetAllAsync(CancellationToken.None));
        Assert.True(await processedStore.ExistsAsync("reserve:ORD-1001", CancellationToken.None));
    }

    [Fact]
    public async Task QueueProcessor_FinalFailure_DeadLetters()
    {
        var inventoryClient = new FakeInventoryClient { ThrowOnReserve = true };
        var processedStore = new InMemoryProcessedOrderStore();
        var deadLetterStore = new InMemoryDeadLetterStore();
        var sut = BuildProcessor(inventoryClient, processedStore, deadLetterStore);

        await sut.ProcessAsync(new PaymentConfirmedEvent("ORD-1002", "CUST-002", ["ITEM-X"], 59.90m, DateTime.UtcNow, "CORR-1002"), CancellationToken.None);

        Assert.Empty(inventoryClient.Reserved);
        var deadLetter = Assert.Single(await deadLetterStore.GetAllAsync(CancellationToken.None));
        Assert.Equal("ORD-1002", deadLetter.OriginalMessage.OrderId);
        Assert.False(await processedStore.ExistsAsync("reserve:ORD-1002", CancellationToken.None));
    }

    [Fact]
    public async Task ProcessAsync_AlreadyProcessed_SkipsReservation()
    {
        var inventoryClient = new FakeInventoryClient();
        var processedStore = new InMemoryProcessedOrderStore();
        await processedStore.MarkProcessedAsync("reserve:ORD-003", CancellationToken.None);
        var sut = BuildProcessor(inventoryClient, processedStore);

        await sut.ProcessAsync(MakeEvent("ORD-003"), CancellationToken.None);

        Assert.Empty(inventoryClient.Reserved);
    }

    [Fact]
    public async Task ProcessAsync_DeadLetter_CapturesFailureReasonAndItems()
    {
        var inventoryClient = new FakeInventoryClient { ThrowOnReserve = true };
        var deadLetterStore = new InMemoryDeadLetterStore();
        var sut = BuildProcessor(inventoryClient, deadLetterStore: deadLetterStore);

        var message = new PaymentConfirmedEvent("ORD-004", "CUST-004", ["ITEM-X", "ITEM-Y"], 75m, DateTime.UtcNow, "CORR-1004");
        await sut.ProcessAsync(message, CancellationToken.None);

        var deadLetter = Assert.Single(await deadLetterStore.GetAllAsync(CancellationToken.None));
        Assert.Equal("Reserve failed.", deadLetter.FailureReason);
        Assert.Equal(["ITEM-X", "ITEM-Y"], deadLetter.OriginalMessage.Items);
        Assert.Equal(1, deadLetter.RetryCount);
    }

    [Fact]
    public async Task ProcessAsync_MultipleDistinctOrders_AllReserved()
    {
        var inventoryClient = new FakeInventoryClient();
        var sut = BuildProcessor(inventoryClient);

        await sut.ProcessAsync(MakeEvent("ORD-005"), CancellationToken.None);
        await sut.ProcessAsync(MakeEvent("ORD-006"), CancellationToken.None);
        await sut.ProcessAsync(MakeEvent("ORD-007"), CancellationToken.None);

        Assert.Equal(3, inventoryClient.Reserved.Count);
        Assert.Contains(inventoryClient.Reserved, r => r.OrderId == "ORD-005");
        Assert.Contains(inventoryClient.Reserved, r => r.OrderId == "ORD-006");
        Assert.Contains(inventoryClient.Reserved, r => r.OrderId == "ORD-007");
    }

    [Fact]
    public async Task ProcessAsync_ConcurrentDuplicateEvents_ReservesOnlyOnce()
    {
        var inventoryClient = new FakeInventoryClient();
        var sut = BuildProcessor(inventoryClient);
        var message = MakeEvent("ORD-008");

        await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => sut.ProcessAsync(message, CancellationToken.None)));

        Assert.Equal(1, inventoryClient.ReserveCallCount);
    }
}