using Microsoft.EntityFrameworkCore;
using OrderFlow.Domain;

namespace OrderFlow.Infrastructure;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<Order>();
        order.HasKey(item => item.Id);
        order.Property(item => item.Customer).HasMaxLength(120).IsRequired();
        order.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        order.Ignore(item => item.Total);
        order.HasMany(item => item.Lines)
            .WithOne()
            .HasForeignKey("OrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        order.Navigation(item => item.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        var line = modelBuilder.Entity<OrderLine>();
        line.HasKey(item => item.Id);
        line.Property(item => item.Product).HasMaxLength(160).IsRequired();
        line.Property(item => item.UnitPrice).HasPrecision(18, 2);
        line.Ignore(item => item.LineTotal);
    }
}
