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

    [Fact]
    public void Duplicate_detection_handles_empty_unique_and_repeated_numbers()
    {
        Assert.False(OrderAlgorithms.ContainsDuplicateOrderNumber([]));
        Assert.False(OrderAlgorithms.ContainsDuplicateOrderNumber([101, 102, 103]));
        Assert.True(OrderAlgorithms.ContainsDuplicateOrderNumber([101, 102, 101]));
    }

    [Fact]
    public void Duplicate_detection_rejects_a_null_sequence()
    {
        Assert.Throws<ArgumentNullException>(() => OrderAlgorithms.ContainsDuplicateOrderNumber(null!));
    }

    [Theory]
    [InlineData("OF-11-FO", true)]
    [InlineData("Never odd or even", true)]
    [InlineData("OF-1234", false)]
    public void Palindrome_check_normalises_case_and_punctuation(string value, bool expected)
    {
        Assert.Equal(expected, OrderAlgorithms.IsPalindromeOrderReference(value));
    }

    [Theory]
    [InlineData("({[]})", true)]
    [InlineData("([)]", false)]
    [InlineData("(()", false)]
    [InlineData("order-{reference}", true)]
    public void Grouping_validation_detects_wrong_order_and_unfinished_pairs(string value, bool expected)
    {
        Assert.Equal(expected, OrderAlgorithms.HasBalancedGrouping(value));
    }

    [Fact]
    public void Sliding_window_returns_the_largest_consecutive_total()
    {
        Assert.Equal(16m, OrderAlgorithms.HighestWindowTotal([4m, 8m, 2m, 10m, 6m], 2));
        Assert.Equal(30m, OrderAlgorithms.HighestWindowTotal([4m, 8m, 2m, 10m, 6m], 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Sliding_window_rejects_an_invalid_window_size(int windowSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OrderAlgorithms.HighestWindowTotal([1m, 2m, 3m], windowSize));
    }
}
