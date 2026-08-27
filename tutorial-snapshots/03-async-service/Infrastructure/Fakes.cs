using OrderFlow.Stage03.Application;
using OrderFlow.Stage03.Domain;

namespace OrderFlow.Stage03.Infrastructure;

public sealed class InMemoryAsyncOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = [];

    public int SaveCount { get; private set; }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await Task.Delay(5, cancellationToken);
        if (!_orders.TryAdd(order.Id, order))
            throw new InvalidOperationException($"Order {order.Id} already exists.");
    }

    public async Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        await Task.Delay(5, cancellationToken);
        return _orders.GetValueOrDefault(id);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(5, cancellationToken);
        SaveCount++;
    }
}

public sealed class SpyReceiptSender : IReceiptSender
{
    public int SendCount { get; private set; }

    public async Task SendAsync(Order order, CancellationToken cancellationToken)
    {
        await Task.Delay(5, cancellationToken);
        SendCount++;
    }
}
