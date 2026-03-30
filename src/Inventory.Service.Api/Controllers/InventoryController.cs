using Inventory.Service.Api.Models;
using Inventory.Service.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Orders;

namespace Inventory.Service.Api.Controllers;

[ApiController]
[Route("inventory")]
public class InventoryController(InventoryOperationsStore operationsStore, ILogger<InventoryController> logger) : ControllerBase
{
    [HttpPost("allocate")]
    [ProducesResponseType(typeof(InventoryOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<InventoryOperationResponse> Allocate([FromBody] InventoryAllocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            return BadRequest("OrderId is required.");
        }

        operationsStore.AddAllocated(request);

        logger.LogInformation(
            "Inventory allocation successful for OrderId {OrderId} with {ItemCount} items. CorrelationId: {CorrelationId}",
            request.OrderId,
            request.Items.Length,
            request.CorrelationId);

        return Ok(new InventoryOperationResponse(
            Success: true,
            Operation: "allocate",
            OrderId: request.OrderId,
            Message: "Inventory allocated successfully."));
    }

    [HttpPost("reserve")]
    [ProducesResponseType(typeof(InventoryOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<InventoryOperationResponse> Reserve([FromBody] InventoryAllocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            return BadRequest("OrderId is required.");
        }

        operationsStore.AddReserved(request);

        logger.LogInformation(
            "Inventory reservation successful for OrderId {OrderId} with {ItemCount} items. CorrelationId: {CorrelationId}",
            request.OrderId,
            request.Items.Length,
            request.CorrelationId);

        return Ok(new InventoryOperationResponse(
            Success: true,
            Operation: "reserve",
            OrderId: request.OrderId,
            Message: "Inventory reserved successfully."));
    }
}