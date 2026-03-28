using Microsoft.AspNetCore.Mvc;
using Order.Orchestrator.Api.Application.Interfaces;

namespace Order.Orchestrator.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(
    IInventoryClient inventoryClient,
    IOrderQueue orderQueue) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var inventoryHealthy = await inventoryClient.IsHealthyAsync(cancellationToken);
        var queueHealthy = await orderQueue.IsHealthyAsync(cancellationToken);

        var status = inventoryHealthy && queueHealthy
            ? "healthy"
            : inventoryHealthy || queueHealthy
                ? "degraded"
                : "unhealthy";

        return Ok(new
        {
            status,
            inventory = inventoryHealthy ? "healthy" : "unhealthy",
            queue = queueHealthy ? "healthy" : "unhealthy"
        });
    }
}