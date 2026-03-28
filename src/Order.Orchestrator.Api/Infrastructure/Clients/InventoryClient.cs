using System.Net.Http.Json;
using Order.Orchestrator.Api.Application.Interfaces;
using Shared.Contracts.Orders;

namespace Order.Orchestrator.Api.Infrastructure.Clients;

public sealed class InventoryClient(HttpClient httpClient) : IInventoryClient
{
    public async Task AllocateAsync(InventoryAllocationRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/inventory/allocate",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task ReserveAsync(InventoryAllocationRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/inventory/reserve",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}