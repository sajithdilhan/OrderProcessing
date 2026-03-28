using Microsoft.AspNetCore.Mvc;
using Oms.Api.Services;
using Shared.Contracts.Orders;

namespace Oms.Api.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController(
    PendingOrdersStore pendingOrdersStore,
    ILogger<OrdersController> logger) : ControllerBase
{
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<PendingOrder>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<PendingOrder>> GetPendingOrders()
    {
        var orders = pendingOrdersStore.GetPendingOrders();

        logger.LogInformation("Returning {OrderCount} pending orders from OMS stub",  orders.Count);

        return Ok(orders);
    }
}