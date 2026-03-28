namespace Order.Orchestrator.Api.Application.Interfaces;

using Order.Orchestrator.Api.Application.Models;

public interface IDeadLetterStore
{
    Task AddAsync(DeadLetterMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<DeadLetterMessage>> GetAllAsync(CancellationToken cancellationToken);
}