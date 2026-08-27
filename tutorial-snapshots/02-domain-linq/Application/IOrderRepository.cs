using OrderFlow.Stage02.Domain;

namespace OrderFlow.Stage02.Application;

public interface IOrderRepository
{
    void Add(Order order);
    Order? Find(Guid id);
    IReadOnlyList<Order> List();
}
