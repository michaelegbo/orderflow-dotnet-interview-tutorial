using Microsoft.EntityFrameworkCore;
using OrderFlow.Application;
using OrderFlow.Domain;

namespace OrderFlow.Infrastructure;

public sealed class EfOrderRepository(OrderDbContext dbContext) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken cancellationToken) =>
        await dbContext.Orders.AddAsync(order, cancellationToken);

    public Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders
            .Include(order => order.Lines)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
