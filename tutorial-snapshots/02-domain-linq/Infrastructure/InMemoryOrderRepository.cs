using OrderFlow.Stage02.Application;
using OrderFlow.Stage02.Domain;

namespace OrderFlow.Stage02.Infrastructure;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, Order> _orders = [];

    public void Add(Order order)
    {
        if (!_orders.TryAdd(order.Id, order))
            throw new InvalidOperationException($"Order {order.Id} already exists.");
    }

    public Order? Find(Guid id) => _orders.GetValueOrDefault(id);

    public IReadOnlyList<Order> List() => _orders.Values.ToList();
}
