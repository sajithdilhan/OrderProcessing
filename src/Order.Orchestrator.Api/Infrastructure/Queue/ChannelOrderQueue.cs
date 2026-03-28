using System.Threading.Channels;
using Order.Orchestrator.Api.Application.Interfaces;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Api.Infrastructure.Queue;

public sealed class ChannelOrderQueue : IOrderQueue
{
    private readonly Channel<PaymentConfirmedEvent> _channel;

    public ChannelOrderQueue()
    {
        _channel = Channel.CreateUnbounded<PaymentConfirmedEvent>();
    }

    public ValueTask EnqueueAsync(PaymentConfirmedEvent message, CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public ValueTask<PaymentConfirmedEvent> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}