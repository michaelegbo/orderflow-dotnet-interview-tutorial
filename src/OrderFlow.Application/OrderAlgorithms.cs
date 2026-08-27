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

    public static bool ContainsDuplicateOrderNumber(IEnumerable<int> orderNumbers)
    {
        ArgumentNullException.ThrowIfNull(orderNumbers);

        var seen = new HashSet<int>();

        foreach (var number in orderNumbers)
        {
            if (!seen.Add(number))
                return true;
        }

        return false;
    }

    public static bool IsPalindromeOrderReference(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalised = new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());

        for (var left = 0; left < normalised.Length / 2; left++)
        {
            if (normalised[left] != normalised[normalised.Length - 1 - left])
                return false;
        }

        return true;
    }

    public static bool HasBalancedGrouping(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var expected = new Stack<char>();

        foreach (var character in value)
        {
            if (character == '(')
                expected.Push(')');
            else if (character == '[')
                expected.Push(']');
            else if (character == '{')
                expected.Push('}');
            else if (character is ')' or ']' or '}')
            {
                if (expected.Count == 0 || expected.Pop() != character)
                    return false;
            }
        }

        return expected.Count == 0;
    }

    public static decimal HighestWindowTotal(IReadOnlyList<decimal> totals, int windowSize)
    {
        ArgumentNullException.ThrowIfNull(totals);

        if (windowSize <= 0 || windowSize > totals.Count)
            throw new ArgumentOutOfRangeException(nameof(windowSize));

        var current = totals.Take(windowSize).Sum();
        var best = current;

        for (var right = windowSize; right < totals.Count; right++)
        {
            current += totals[right] - totals[right - windowSize];
            best = Math.Max(best, current);
        }

        return best;
    }
}
