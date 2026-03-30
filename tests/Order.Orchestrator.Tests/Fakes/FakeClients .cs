using System.Collections.Concurrent;
using Order.Orchestrator.Api.Application.Interfaces;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Tests.Fakes;

internal sealed class FakeOmsClient : IOmsClient
{
    private readonly IReadOnlyList<PendingOrder> _orders;
    private readonly TimeSpan _delay;

    public FakeOmsClient(IReadOnlyList<PendingOrder>? orders = null, TimeSpan? delay = null)
    {
        _orders = orders ?? [];
        _delay = delay ?? TimeSpan.Zero;
    }

    public async Task<IReadOnlyList<PendingOrder>> GetPendingOrdersAsync(CancellationToken cancellationToken)
    {
        if (_delay > TimeSpan.Zero)
        {
            await Task.Delay(_delay, cancellationToken);
        }

        return _orders;
    }
}

internal sealed class FakeInventoryClient : IInventoryClient
{
    public ConcurrentBag<InventoryAllocationRequest> Allocated { get; } = new();
    public ConcurrentBag<InventoryAllocationRequest> Reserved { get; } = new();

    public int ReserveCallCount => _reserveCallCount;
    private int _reserveCallCount;

    public bool ThrowOnReserve { get; set; }
    public bool ThrowOnAllocate { get; set; }
    public bool IsHealthyResult { get; set; } = true;

    public Task AllocateAsync(InventoryAllocationRequest request, CancellationToken cancellationToken)
    {
        if (ThrowOnAllocate)
        {
            throw new InvalidOperationException("Allocate failed.");
        }

        Allocated.Add(request);
        return Task.CompletedTask;
    }

    public Task ReserveAsync(InventoryAllocationRequest request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _reserveCallCount);

        if (ThrowOnReserve)
        {
            throw new InvalidOperationException("Reserve failed.");
        }

        Reserved.Add(request);
        return Task.CompletedTask;
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
        => Task.FromResult(IsHealthyResult);
}

internal static class TestWait
{
    public static async Task UntilAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null)
    {
        var stopAt = DateTime.UtcNow + timeout;
        var delay = pollInterval ?? TimeSpan.FromMilliseconds(50);

        while (DateTime.UtcNow < stopAt)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(delay);
        }

        throw new TimeoutException("Condition was not met within the timeout.");
    }
}