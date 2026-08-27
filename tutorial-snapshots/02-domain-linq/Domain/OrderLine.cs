namespace OrderFlow.Stage02.Domain;

public sealed class OrderLine
{
    public OrderLine(string product, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(product))
            throw new ArgumentException("Product is required.", nameof(product));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        Product = product.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public string Product { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }
    public decimal LineTotal => Quantity * UnitPrice;
}
