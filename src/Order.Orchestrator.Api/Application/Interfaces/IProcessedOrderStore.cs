namespace Order.Orchestrator.Api.Application.Interfaces;

public interface IProcessedOrderStore
{
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);
    Task MarkProcessedAsync(string key, CancellationToken cancellationToken);
}