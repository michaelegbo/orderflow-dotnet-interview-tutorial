using OrderFlow.Application;
using OrderFlow.Domain;

namespace OrderFlow.UnitTests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task Create_builds_and_saves_an_order()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderService(repository, new SpyReceiptSender(), new FixedTimeProvider());
        var command = new CreateOrderCommand("Ada", [new("Keyboard", 2, 40m)]);

        var created = await service.CreateAsync(command, CancellationToken.None);

        Assert.Equal(80m, created.Total);
        Assert.Equal(DateTimeOffset.UnixEpoch, created.CreatedAtUtc);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Paying_an_order_sends_one_receipt_even_when_retried()
    {
        var order = new Order("Ada", DateTimeOffset.UnixEpoch);
        order.AddLine("Keyboard", 1, 40m);
        var repository = new FakeOrderRepository(order);
        var receipt = new SpyReceiptSender();
        var service = new OrderService(repository, receipt, new FixedTimeProvider());

        await service.MarkPaidAsync(order.Id, CancellationToken.None);
        await service.MarkPaidAsync(order.Id, CancellationToken.None);

        Assert.Equal(1, receipt.SendCount);
        Assert.Equal(1, repository.SaveCount);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch;
    }

    private sealed class SpyReceiptSender : IReceiptSender
    {
        public int SendCount { get; private set; }
        public Task SendAsync(Order order, CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOrderRepository(params Order[] seed) : IOrderRepository
    {
        private readonly List<Order> _orders = [.. seed];
        public int SaveCount { get; private set; }

        public Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            _orders.Add(order);
            return Task.CompletedTask;
        }

        public Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_orders.SingleOrDefault(order => order.Id == id));

        public Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Order>>(_orders);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
