using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Order.Orchestrator.Api.Application.Services;
using Order.Orchestrator.Api.Controllers;
using Order.Orchestrator.Api.Infrastructure.Storage;
using Order.Orchestrator.Tests.Fakes;
using Shared.Contracts.Orders;
using System.Diagnostics;

namespace Order.Orchestrator.Tests.Unit;

public sealed class OrdersControllerTests
{
    private static OrdersController BuildController(
        FakeOmsClient? omsClient = null,
        FakeInventoryClient? inventoryClient = null,
        InMemoryProcessedOrderStore? store = null)
    {
        var service = new PendingOrderSyncService(
            omsClient ?? new FakeOmsClient(),
            inventoryClient ?? new FakeInventoryClient(),
            store ?? new InMemoryProcessedOrderStore(),
            NullLogger<PendingOrderSyncService>.Instance);

        return new OrdersController(service, NullLogger<OrdersController>.Instance);
    }

    [Fact]
    public void PingHandler_ReturnsImmediately()
    {
        var controller = BuildController(omsClient: new FakeOmsClient(delay: TimeSpan.FromSeconds(2)));

        var sw = Stopwatch.StartNew();
        var result = controller.PendingPing(new PendingPingRequest("corr-001"));
        sw.Stop();

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(202, accepted.StatusCode);
        Assert.True(sw.ElapsedMilliseconds < 200, $"Expected immediate return but took {sw.ElapsedMilliseconds}ms");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PendingPing_MissingCorrelationId_ReturnsBadRequest(string? correlationId)
    {
        var controller = BuildController();

        var result = controller.PendingPing(new PendingPingRequest(correlationId!));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, bad.StatusCode);
    }

    [Fact]
    public async Task PendingPing_TriggersInventoryAllocation_ForEachPendingOrder()
    {
        var orders = new[]
        {
            new PendingOrder("order-1", "cust-1", ["item-a"], 10m),
            new PendingOrder("order-2", "cust-2", ["item-b"], 20m),
        };

        var inventoryClient = new FakeInventoryClient();
        var controller = BuildController(
            omsClient: new FakeOmsClient(orders),
            inventoryClient: inventoryClient);

        controller.PendingPing(new PendingPingRequest("corr-002"));

        await TestWait.UntilAsync(
            () => inventoryClient.Allocated.Count == 2,
            timeout: TimeSpan.FromSeconds(3));

        Assert.Contains(inventoryClient.Allocated, r => r.OrderId == "order-1");
        Assert.Contains(inventoryClient.Allocated, r => r.OrderId == "order-2");
    }

    [Fact]
    public async Task PendingPing_SkipsAlreadyProcessedOrders()
    {
        var order = new PendingOrder("order-3", "cust-3", ["item-c"], 30m);
        var store = new InMemoryProcessedOrderStore();
        await store.MarkProcessedAsync("allocate:order-3", CancellationToken.None);

        var inventoryClient = new FakeInventoryClient();
        var controller = BuildController(
            omsClient: new FakeOmsClient([order]),
            inventoryClient: inventoryClient,
            store: store);

        controller.PendingPing(new PendingPingRequest("corr-003"));

        await Task.Delay(300);

        Assert.Empty(inventoryClient.Allocated);
    }

    [Fact]
    public async Task PendingPing_NoPendingOrders_NoAllocationsMade()
    {
        var inventoryClient = new FakeInventoryClient();
        var controller = BuildController(
            omsClient: new FakeOmsClient([]),
            inventoryClient: inventoryClient);

        controller.PendingPing(new PendingPingRequest("corr-004"));

        await Task.Delay(300);

        Assert.Empty(inventoryClient.Allocated);
    }

    [Fact]
    public async Task PendingPing_DoesNotAllocateSameOrderTwiceOnSecondPing()
    {
        var order = new PendingOrder("order-4", "cust-4", ["item-d"], 40m);
        var inventoryClient = new FakeInventoryClient();
        var controller = BuildController(
            omsClient: new FakeOmsClient([order]),
            inventoryClient: inventoryClient);

        controller.PendingPing(new PendingPingRequest("corr-005a"));
        await TestWait.UntilAsync(() => inventoryClient.Allocated.Count == 1, TimeSpan.FromSeconds(3));

        controller.PendingPing(new PendingPingRequest("corr-005b"));
        await Task.Delay(300);

        Assert.Single(inventoryClient.Allocated);
    }
}