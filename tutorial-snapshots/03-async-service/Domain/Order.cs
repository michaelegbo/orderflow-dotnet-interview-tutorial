namespace OrderFlow.Stage03.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public Order(string customer, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(customer))
            throw new ArgumentException("Customer is required.", nameof(customer));

        Id = Guid.NewGuid();
        Customer = customer.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public string Customer { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public bool IsPaid { get; private set; }
    public IReadOnlyList<OrderLine> Lines => _lines;
    public decimal Total => _lines.Sum(line => line.Quantity * line.UnitPrice);

    public void AddLine(string product, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(product))
            throw new ArgumentException("Product is required.", nameof(product));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        _lines.Add(new OrderLine(product.Trim(), quantity, unitPrice));
    }

    public bool MarkPaid()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException("An empty order cannot be paid.");
        if (IsPaid)
            return false;

        IsPaid = true;
        return true;
    }
}

public sealed record OrderLine(string Product, int Quantity, decimal UnitPrice);
