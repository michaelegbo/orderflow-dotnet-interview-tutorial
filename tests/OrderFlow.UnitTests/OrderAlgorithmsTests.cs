using OrderFlow.Application;

namespace OrderFlow.UnitTests;

public sealed class OrderAlgorithmsTests
{
    [Fact]
    public void Pair_search_returns_two_different_matching_positions()
    {
        var match = OrderAlgorithms.FindPairWithTargetTotal([20m, 35m, 80m, 65m], 100m);

        Assert.Equal((0, 2), match);
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(30, 2)]
    [InlineData(50, 4)]
    [InlineData(99, -1)]
    public void Binary_search_finds_edges_middle_or_missing(int target, int expected)
    {
        Assert.Equal(expected, OrderAlgorithms.BinarySearch([10, 20, 30, 40, 50], target));
    }
}
