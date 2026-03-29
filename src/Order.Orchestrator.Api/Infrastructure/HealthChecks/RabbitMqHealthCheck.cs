using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using Shared.Contracts.Common;

namespace Order.Orchestrator.Api.HealthChecks;

public sealed class RabbitMqHealthCheck(RabbitMqSettings settings) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                VirtualHost = settings.VirtualHost,
                UserName = settings.Username,
                Password = settings.Password
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("RabbitMQ is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is unreachable.", ex);
        }
    }
}