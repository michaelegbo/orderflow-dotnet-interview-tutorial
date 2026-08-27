using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> List(CancellationToken cancellationToken) =>
        Ok(await orderService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderService.GetAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [Authorize(Policy = "OrderManager")]
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.Customer,
            request.Lines.Select(line => new CreateOrderLineCommand(
                line.Product,
                line.Quantity,
                line.UnitPrice)).ToList());

        var created = await orderService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [Authorize(Policy = "OrderManager")]
    [HttpPut("{id:guid}/pay")]
    public async Task<ActionResult<OrderDto>> MarkPaid(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await orderService.MarkPaidAsync(id, cancellationToken));
}

public sealed class CreateOrderRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Customer { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public IReadOnlyCollection<CreateOrderLineRequest> Lines { get; init; } = [];
}

public sealed class CreateOrderLineRequest
{
    [Required, StringLength(160, MinimumLength = 1)]
    public string Product { get; init; } = string.Empty;

    [Range(1, 10_000)]
    public int Quantity { get; init; }

    [Range(typeof(decimal), "0.01", "1000000000")]
    public decimal UnitPrice { get; init; }
}
