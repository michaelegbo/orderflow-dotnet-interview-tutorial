using OrderFlow.Stage03.Application;
using OrderFlow.Stage03.Infrastructure;

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var repository = new InMemoryAsyncOrderRepository();
var receipts = new SpyReceiptSender();
var service = new OrderService(repository, receipts, TimeProvider.System);

var order = await service.CreateAsync("Ada", timeout.Token);
order.AddLine("Keyboard", 2, 40m);

// These operations are independent. Creating both tasks first lets WhenAll
// compose their completion; WhenAll itself does not start the work.
var firstRead = service.GetAsync(order.Id, timeout.Token);
var secondRead = service.GetAsync(order.Id, timeout.Token);
var reads = await Task.WhenAll(firstRead, secondRead);

await service.MarkPaidAsync(order.Id, timeout.Token);
await service.MarkPaidAsync(order.Id, timeout.Token);

if (reads.Any(result => result is null) || receipts.SendCount != 1)
    throw new InvalidOperationException("The Stage 03 async/idempotency self-check failed.");

Console.WriteLine($"{order.Customer}: {order.Total:C} | Paid: {order.IsPaid}");
Console.WriteLine("STAGE 03 PASS — async, cancellation and retry-safe state transition");
