using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application;

namespace OrderFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderFlowInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<OrderDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddSingleton<IReceiptSender, ConsoleReceiptSender>();
        return services;
    }
}
