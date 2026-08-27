using OrderFlow.Domain;

namespace OrderFlow.Application;

public sealed class OrderService(
    IOrderRepository repository,
    IReceiptSender receiptSender,
    TimeProvider clock) : IOrderService
{
    public async Task<OrderDto> CreateAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Lines.Count == 0)
            throw new ArgumentException("At least one line is required.", nameof(command));

        var order = new Order(command.Customer, clock.GetUtcNow());
        foreach (var line in command.Lines)
            order.AddLine(line.Product, line.Quantity, line.UnitPrice);

        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(order);
    }

    public async Task<OrderDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await repository.FindAsync(id, cancellationToken);
        return order is null ? null : ToDto(order);
    }

    public async Task<IReadOnlyList<OrderDto>> ListAsync(CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken)).Select(ToDto).ToList();

    public async Task<OrderDto> MarkPaidAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await repository.FindAsync(id, cancellationToken)
            ?? throw new OrderNotFoundException(id);

        if (order.MarkPaid())
        {
            await repository.SaveChangesAsync(cancellationToken);
            await receiptSender.SendAsync(order, cancellationToken);
        }

        return ToDto(order);
    }

    private static OrderDto ToDto(Order order) => new(
        order.Id,
        order.Customer,
        order.Status,
        order.CreatedAtUtc,
        order.Total,
        order.Lines.Select(line => new OrderLineDto(
            line.Product,
            line.Quantity,
            line.UnitPrice,
            line.LineTotal)).ToList());
}
