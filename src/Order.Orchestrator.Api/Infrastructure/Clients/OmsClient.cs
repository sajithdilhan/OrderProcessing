using System.Net.Http.Json;
using Order.Orchestrator.Api.Application.Interfaces;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Api.Infrastructure.Clients;

public sealed class OmsClient(HttpClient httpClient) : IOmsClient
{
    public async Task<IReadOnlyList<PendingOrder>> GetPendingOrdersAsync(CancellationToken cancellationToken)
    {
        var result = await httpClient.GetFromJsonAsync<List<PendingOrder>>(
            "/orders/pending",
            cancellationToken);

        return result ?? [];
    }
}