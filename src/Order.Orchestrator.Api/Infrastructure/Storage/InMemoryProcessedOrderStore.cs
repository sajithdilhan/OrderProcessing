using System.Collections.Concurrent;
using Order.Orchestrator.Api.Application.Interfaces;

namespace Order.Orchestrator.Api.Infrastructure.Storage;

public sealed class InMemoryProcessedOrderStore : IProcessedOrderStore
{
    private readonly ConcurrentDictionary<string, byte> _processed = new();

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        return Task.FromResult(_processed.ContainsKey(key));
    }

    public Task MarkProcessedAsync(string key, CancellationToken cancellationToken)
    {
        _processed.TryAdd(key, 0);
        return Task.CompletedTask;
    }
}