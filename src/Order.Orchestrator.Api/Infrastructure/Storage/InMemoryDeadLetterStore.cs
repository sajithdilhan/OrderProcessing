using Order.Orchestrator.Api.Application.Interfaces;
using Order.Orchestrator.Api.Application.Models;

namespace Order.Orchestrator.Api.Infrastructure.Storage;

public sealed class InMemoryDeadLetterStore : IDeadLetterStore
{
    private readonly List<DeadLetterMessage> _messages = [];
    private readonly object _lock = new();

    public Task AddAsync(DeadLetterMessage message, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _messages.Add(message);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DeadLetterMessage>> GetAllAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<DeadLetterMessage>>(_messages.ToList().AsReadOnly());
        }
    }
}