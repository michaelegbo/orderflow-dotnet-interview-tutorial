using Microsoft.Extensions.Logging;
using OrderFlow.Application;
using OrderFlow.Domain;

namespace OrderFlow.Infrastructure;

public sealed class ConsoleReceiptSender(ILogger<ConsoleReceiptSender> logger) : IReceiptSender
{
    public Task SendAsync(Order order, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Receipt queued for order {OrderId} with total {Total:C}.",
            order.Id,
            order.Total);
        return Task.CompletedTask;
    }
}
