using Microsoft.Extensions.Diagnostics.HealthChecks;
using Order.Orchestrator.Api.Application.Interfaces;

namespace Order.Orchestrator.Api.HealthChecks;

public sealed class InventoryHealthCheck(IInventoryClient inventoryClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await inventoryClient.IsHealthyAsync(cancellationToken);

            if (isHealthy)
            {
                return HealthCheckResult.Healthy("Inventory service is reachable.");
            }

            return HealthCheckResult.Degraded("Inventory service responded as unhealthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Inventory service health check failed.", ex);
        }
    }
}