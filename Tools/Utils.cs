
public static class Utils
{
    private static Random Random = new Random();

    public static IOrderedEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerator)
    {
        return enumerator.OrderBy(a => Random.Next());
    }

    public static double Within(this Random random, double bound)
    {
        return random.Within(0, bound);
    }

    public static double Within(this Random random, double firstBound, double secondBound)
    {
        var delta = firstBound - secondBound;
        var value = random.NextDouble();

        return firstBound - delta * value;
    }

    public static int Within(this Random random, int bound)
    {
        return random.Within(0, bound);
    }

    public static int Within(this Random random, int firstBound, int secondBound)
    {
        var delta = firstBound - secondBound;
        var value = random.Next(Math.Abs(delta));

        return firstBound + (secondBound > firstBound ? 1 : -1) * value;
    }
}