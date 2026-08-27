namespace OrderFlow.Domain;

public sealed class OrderLine
{
    private OrderLine() { }

    internal OrderLine(string product, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(product))
            throw new ArgumentException("Product is required.", nameof(product));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (unitPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price must be positive.");

        Product = product.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public int Id { get; private set; }
    public string Product { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => Quantity * UnitPrice;
}
