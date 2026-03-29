using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Order.Orchestrator.Api.Application.Services;
using Order.Orchestrator.Api.Controllers;
using Order.Orchestrator.Api.Infrastructure.Storage;
using Order.Orchestrator.Tests.TestDoubles;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Tests.Unit;

public sealed class OrdersControllerTests
{
    [Fact]
    public void PingHandler_ReturnsImmediately()
    {
        var omsClient = new FakeOmsClient(
            orders: [],
            delay: TimeSpan.FromSeconds(2));

        var inventoryClient = new FakeInventoryClient();

        var service = new PendingOrderSyncService(
            omsClient,
            inventoryClient,
            new InMemoryProcessedOrderStore(),
            NullLogger<PendingOrderSyncService>.Instance);

        var controller = new OrdersController(
            service,
            NullLogger<OrdersController>.Instance);

        var request = new PendingPingRequest("corr-001");

        var sw = Stopwatch.StartNew();
        var result = controller.PendingPing(request);
        sw.Stop();

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(202, accepted.StatusCode);
        Assert.True(sw.ElapsedMilliseconds < 200, $"Expected immediate return but took {sw.ElapsedMilliseconds}ms");
    }
}