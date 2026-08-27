using OrderFlow.Stage02.Application;
using OrderFlow.Stage02.Domain;
using OrderFlow.Stage02.Infrastructure;

IOrderRepository repository = new InMemoryOrderRepository();

var first = NewOrder("Ada", "Keyboard", 2, 40m, paid: true);
var second = NewOrder("Grace", "Monitor", 1, 125m, paid: true);
var third = NewOrder("Linus", "Cable", 2, 15m, paid: false);

first.AddLine("Mouse", 1, 10m);

repository.Add(first);
repository.Add(second);
repository.Add(third);

var highValuePaidOrders = repository.List()
    .Where(order => order.IsPaid && order.Total >= 75m)
    .OrderByDescending(order => order.Total)
    .Select(order => new { order.Customer, order.Total })
    .ToList();

if (highValuePaidOrders.Count != 2 || highValuePaidOrders[0].Customer != "Grace")
    throw new InvalidOperationException("The Stage 02 LINQ self-check failed.");

foreach (var order in highValuePaidOrders)
    Console.WriteLine($"{order.Customer}: {order.Total:C}");

Console.WriteLine("STAGE 02 PASS — objects, interface, collections and LINQ");

static Order NewOrder(
    string customer,
    string product,
    int quantity,
    decimal unitPrice,
    bool paid)
{
    var order = new Order(customer);
    order.AddLine(product, quantity, unitPrice);
    if (paid)
        order.MarkPaid();
    return order;
}
