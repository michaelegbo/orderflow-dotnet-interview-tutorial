using OrderFlow.Domain;

namespace OrderFlow.Application;

public sealed record CreateOrderLineCommand(string Product, int Quantity, decimal UnitPrice);
public sealed record CreateOrderCommand(string Customer, IReadOnlyCollection<CreateOrderLineCommand> Lines);
public sealed record OrderLineDto(string Product, int Quantity, decimal UnitPrice, decimal LineTotal);
public sealed record OrderDto(
    Guid Id,
    string Customer,
    OrderStatus Status,
    DateTimeOffset CreatedAtUtc,
    decimal Total,
    IReadOnlyCollection<OrderLineDto> Lines);

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken);
    Task<OrderDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderDto>> ListAsync(CancellationToken cancellationToken);
    Task<OrderDto> MarkPaidAsync(Guid id, CancellationToken cancellationToken);
}

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IReceiptSender
{
    Task SendAsync(Order order, CancellationToken cancellationToken);
}

public sealed class OrderNotFoundException(Guid id)
    : Exception($"Order '{id}' was not found.");
