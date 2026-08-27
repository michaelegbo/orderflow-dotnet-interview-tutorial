namespace OrderFlow.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    private Order() { }

    public Order(string customer, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(customer))
            throw new ArgumentException("Customer is required.", nameof(customer));

        Id = Guid.NewGuid();
        Customer = customer.Trim();
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        Status = OrderStatus.Draft;
    }

    public Guid Id { get; private set; }
    public string Customer { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(line => line.LineTotal);

    public void AddLine(string product, int quantity, decimal unitPrice) =>
        _lines.Add(new OrderLine(product, quantity, unitPrice));

    public bool MarkPaid()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException("An empty order cannot be paid.");
        if (Status == OrderStatus.Paid)
            return false;

        Status = OrderStatus.Paid;
        return true;
    }
}
