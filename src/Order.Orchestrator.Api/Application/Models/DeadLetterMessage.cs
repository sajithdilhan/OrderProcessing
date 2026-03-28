using Shared.Contracts.Orders;

namespace Order.Orchestrator.Api.Application.Models;

public sealed record DeadLetterMessage(
    PaymentConfirmedEvent OriginalMessage,
    int RetryCount,
    string FailureReason,
    DateTime FailedAtUtc);