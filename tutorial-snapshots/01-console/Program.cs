string customer = "Sarah";
int quantity = 3;
decimal unitPrice = 25m;
bool isPaid = true;

decimal total = CalculateTotal(quantity, unitPrice);
string size = ClassifyOrder(total);
bool hasValidQuantity = quantity > 0;
bool canFulfil = isPaid && total >= 50m;

if (total >= 100m)
{
    Console.WriteLine("Large order");
}
else
{
    Console.WriteLine("Small order");
}

int[] quantities = [quantity, 2, 1];
int totalQuantity = 0;
foreach (int itemQuantity in quantities)
{
    totalQuantity += itemQuantity;
}

decimal[] sampleTotals = [total, 100m, 55m, 220m, 10m];
foreach (decimal orderTotal in sampleTotals)
{
    if (orderTotal >= 100m)
        Console.WriteLine($"Qualifying total: {orderTotal:C}");
}

Console.WriteLine($"{customer} ordered {quantity} items at {unitPrice:C} each. Total = {total:C}. Paid = {isPaid}");
Console.WriteLine($"Band: {size}");
Console.WriteLine($"Valid quantity: {hasValidQuantity}; can fulfil: {canFulfil}");
Console.WriteLine($"Batch quantity: {totalQuantity}");
Console.WriteLine("STAGE 01 PASS — syntax, control flow and methods");

static decimal CalculateTotal(int quantity, decimal unitPrice)
{
    if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
    if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
    return quantity * unitPrice;
}

static string ClassifyOrder(decimal total) => total switch
{
    >= 500m => "VIP",
    >= 100m => "Standard large",
    _ => "Small"
};
