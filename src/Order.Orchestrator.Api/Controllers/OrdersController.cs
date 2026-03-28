using Microsoft.AspNetCore.Mvc;
using Order.Orchestrator.Api.Application.Services;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Api.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController(
    PendingOrderSyncService pendingOrderSyncService,
    ILogger<OrdersController> logger) : ControllerBase
{
    [HttpPost("pending-ping")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult PendingPing([FromBody] PendingPingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            return BadRequest("CorrelationId is required.");
        }

        logger.LogInformation(
            "Received pending order ping with CorrelationId {CorrelationId}",
            request.CorrelationId);

        _ = pendingOrderSyncService.SyncPendingOrdersAsync(request.CorrelationId, CancellationToken.None);

        return Accepted();
    }
}