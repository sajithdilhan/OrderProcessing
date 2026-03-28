using Microsoft.AspNetCore.Mvc;
using Order.Orchestrator.Api.Application.Interfaces;

namespace Order.Orchestrator.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(
    IInventoryClient inventoryClient) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var inventoryHealthy = await inventoryClient.IsHealthyAsync(cancellationToken);

        var status = inventoryHealthy ? "healthy" : "degraded";

        return Ok(new
        {
            status,
            inventory = inventoryHealthy ? "healthy" : "unhealthy",
            broker = "configured"
        });
    }
}