using OrderFlow.Stage03.Domain;

namespace OrderFlow.Stage03.Application;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IReceiptSender
{
    Task SendAsync(Order order, CancellationToken cancellationToken);
}
