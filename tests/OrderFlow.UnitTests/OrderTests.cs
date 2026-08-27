using OrderFlow.Domain;

namespace OrderFlow.UnitTests;

public sealed class OrderTests
{
    [Fact]
    public void Total_is_the_sum_of_every_line()
    {
        var order = new Order("Ada", DateTimeOffset.UnixEpoch);
        order.AddLine("Keyboard", 2, 40m);
        order.AddLine("Mouse", 1, 25m);

        Assert.Equal(105m, order.Total);
    }

    [Fact]
    public void Empty_order_cannot_be_paid()
    {
        var order = new Order("Ada", DateTimeOffset.UnixEpoch);

        var error = Assert.Throws<InvalidOperationException>(() => order.MarkPaid());

        Assert.Equal("An empty order cannot be paid.", error.Message);
    }

    [Fact]
    public void Paying_twice_is_idempotent()
    {
        var order = new Order("Ada", DateTimeOffset.UnixEpoch);
        order.AddLine("Keyboard", 1, 40m);

        Assert.True(order.MarkPaid());
        Assert.False(order.MarkPaid());
        Assert.Equal(OrderStatus.Paid, order.Status);
    }
}
