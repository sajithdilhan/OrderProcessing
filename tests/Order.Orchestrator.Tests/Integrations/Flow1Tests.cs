using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Order.Orchestrator.Api.Application.Services;
using Order.Orchestrator.Api.Controllers;
using Order.Orchestrator.Api.Infrastructure.Storage;
using Order.Orchestrator.Tests.TestDoubles;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Tests.Integration;

public sealed class Flow1Tests
{
    [Fact]
    public async Task Flow1_OmsPing_InventoryReceives()
    {
        var omsOrders = new[]
        {
            new PendingOrder("ORD-2001", "CUST-101", ["A", "B"], 100m),
            new PendingOrder("ORD-2002", "CUST-102", ["C"], 50m)
        };

        var omsClient = new FakeOmsClient(omsOrders);
        var inventoryClient = new FakeInventoryClient();

        var service = new PendingOrderSyncService(
            omsClient,
            inventoryClient,
            new InMemoryProcessedOrderStore(),
            NullLogger<PendingOrderSyncService>.Instance);

        var controller = new OrdersController(
            service,
            NullLogger<OrdersController>.Instance);

        var result = controller.PendingPing(new PendingPingRequest("corr-flow1"));

        Assert.IsType<AcceptedResult>(result);

        await TestWait.UntilAsync(
            () => inventoryClient.Allocated.Count == 2,
            timeout: TimeSpan.FromSeconds(3));

        Assert.Equal(2, inventoryClient.Allocated.Count);
    }
}