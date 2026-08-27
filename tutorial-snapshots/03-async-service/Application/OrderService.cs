using OrderFlow.Stage03.Domain;

namespace OrderFlow.Stage03.Application;

public sealed class OrderService(
    IOrderRepository repository,
    IReceiptSender receiptSender,
    TimeProvider clock)
{
    public async Task<Order> CreateAsync(
        string customer,
        CancellationToken cancellationToken)
    {
        var order = new Order(customer, clock.GetUtcNow());
        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return order;
    }

    public Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        repository.FindAsync(id, cancellationToken);

    public async Task<Order> MarkPaidAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await repository.FindAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Order {id} was not found.");

        if (order.MarkPaid())
        {
            await repository.SaveChangesAsync(cancellationToken);
            await receiptSender.SendAsync(order, cancellationToken);
        }

        return order;
    }
}
