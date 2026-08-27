namespace OrderFlow.Stage02.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public Order(string customer)
    {
        if (string.IsNullOrWhiteSpace(customer))
            throw new ArgumentException("Customer is required.", nameof(customer));

        Id = Guid.NewGuid();
        Customer = customer.Trim();
    }

    public Guid Id { get; }
    public string Customer { get; }
    public bool IsPaid { get; private set; }
    public IReadOnlyList<OrderLine> Lines => _lines;
    public decimal Total => _lines.Sum(line => line.LineTotal);

    public void AddLine(string product, int quantity, decimal unitPrice) =>
        _lines.Add(new OrderLine(product, quantity, unitPrice));

    public void MarkPaid()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException("An empty order cannot be paid.");

        IsPaid = true;
    }
}
