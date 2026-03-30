using Microsoft.Extensions.Logging.Abstractions;
using Order.Orchestrator.Api.Application.Services;
using Order.Orchestrator.Api.Infrastructure.Storage;
using Order.Orchestrator.Tests.Fakes;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Tests.Unit;

public sealed class PendingOrderSyncServiceTests
{
    private static PendingOrderSyncService BuildService(
        FakeOmsClient? omsClient = null,
        FakeInventoryClient? inventoryClient = null,
        InMemoryProcessedOrderStore? store = null) =>
        new(
            omsClient ?? new FakeOmsClient(),
            inventoryClient ?? new FakeInventoryClient(),
            store ?? new InMemoryProcessedOrderStore(),
            NullLogger<PendingOrderSyncService>.Instance);

    [Fact]
    public async Task SyncPendingOrdersAsync_AllocatesCorrectItemsPerOrder()
    {
        var orders = new[]
        {
            new PendingOrder("ORD-1", "CUST-1", ["SKU-A", "SKU-B"], 50m),
            new PendingOrder("ORD-2", "CUST-2", ["SKU-C"], 25m),
        };

        var inventoryClient = new FakeInventoryClient();
        var sut = BuildService(new FakeOmsClient(orders), inventoryClient);

        await sut.SyncPendingOrdersAsync("corr-1", CancellationToken.None);

        var ord1 = Assert.Single(inventoryClient.Allocated, r => r.OrderId == "ORD-1");
        Assert.Equal(["SKU-A", "SKU-B"], ord1.Items);

        var ord2 = Assert.Single(inventoryClient.Allocated, r => r.OrderId == "ORD-2");
        Assert.Equal(["SKU-C"], ord2.Items);
    }

    [Fact]
    public async Task SyncPendingOrdersAsync_MarksOrderProcessedAfterAllocation()
    {
        var order = new PendingOrder("ORD-3", "CUST-3", ["SKU-D"], 10m);
        var store = new InMemoryProcessedOrderStore();
        var sut = BuildService(new FakeOmsClient([order]), store: store);

        await sut.SyncPendingOrdersAsync("corr-2", CancellationToken.None);

        Assert.True(await store.ExistsAsync("allocate:ORD-3", CancellationToken.None));
    }

    [Fact]
    public async Task SyncPendingOrdersAsync_PartialIdempotency_OnlyAllocatesNewOrders()
    {
        var orders = new[]
        {
            new PendingOrder("ORD-4", "CUST-4", ["SKU-E"], 10m),
            new PendingOrder("ORD-5", "CUST-5", ["SKU-F"], 20m),
        };

        var store = new InMemoryProcessedOrderStore();
        await store.MarkProcessedAsync("allocate:ORD-4", CancellationToken.None);

        var inventoryClient = new FakeInventoryClient();
        var sut = BuildService(new FakeOmsClient(orders), inventoryClient, store);

        await sut.SyncPendingOrdersAsync("corr-3", CancellationToken.None);

        Assert.Single(inventoryClient.Allocated);
        Assert.Contains(inventoryClient.Allocated, r => r.OrderId == "ORD-5");
    }

    [Fact]
    public async Task SyncPendingOrdersAsync_OmsClientThrows_CompletesWithoutException()
    {
        var omsClient = new FakeOmsClient(throwOnGet: true);
        var sut = BuildService(omsClient);

        var ex = await Record.ExceptionAsync(() =>
            sut.SyncPendingOrdersAsync("corr-4", CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task SyncPendingOrdersAsync_AllocateThrows_CompletesWithoutException()
    {
        var order = new PendingOrder("ORD-6", "CUST-6", ["SKU-G"], 30m);
        var inventoryClient = new FakeInventoryClient { ThrowOnAllocate = true };
        var sut = BuildService(new FakeOmsClient([order]), inventoryClient);

        var ex = await Record.ExceptionAsync(() =>
            sut.SyncPendingOrdersAsync("corr-5", CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task SyncPendingOrdersAsync_AllocateThrows_OrderNotMarkedProcessed()
    {
        var order = new PendingOrder("ORD-7", "CUST-7", ["SKU-H"], 40m);
        var store = new InMemoryProcessedOrderStore();
        var inventoryClient = new FakeInventoryClient { ThrowOnAllocate = true };
        var sut = BuildService(new FakeOmsClient([order]), inventoryClient, store);

        await sut.SyncPendingOrdersAsync("corr-6", CancellationToken.None);

        Assert.False(await store.ExistsAsync("allocate:ORD-7", CancellationToken.None));
    }
}
