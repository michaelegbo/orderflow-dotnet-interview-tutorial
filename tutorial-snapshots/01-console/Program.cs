string customer = "Sarah";
int quantity = 3;
decimal unitPrice = 25m;
bool isPaid = true;

decimal total = CalculateTotal(quantity, unitPrice);
string size = ClassifyOrder(total);

Console.WriteLine($"{customer}: {quantity} × {unitPrice:C} = {total:C}");
Console.WriteLine($"Band: {size} | Paid: {isPaid}");
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
