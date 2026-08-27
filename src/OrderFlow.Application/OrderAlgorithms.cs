namespace OrderFlow.Application;

public static class OrderAlgorithms
{
    public static (int First, int Second)? FindPairWithTargetTotal(
        IReadOnlyList<decimal> totals,
        decimal target)
    {
        var seen = new Dictionary<decimal, int>();

        for (var index = 0; index < totals.Count; index++)
        {
            var needed = target - totals[index];
            if (seen.TryGetValue(needed, out var otherIndex))
                return (otherIndex, index);

            seen[totals[index]] = index;
        }

        return null;
    }

    public static int BinarySearch(IReadOnlyList<int> sortedOrderNumbers, int target)
    {
        var left = 0;
        var right = sortedOrderNumbers.Count - 1;

        while (left <= right)
        {
            var middle = left + (right - left) / 2;
            if (sortedOrderNumbers[middle] == target)
                return middle;

            if (sortedOrderNumbers[middle] < target)
                left = middle + 1;
            else
                right = middle - 1;
        }

        return -1;
    }
}
