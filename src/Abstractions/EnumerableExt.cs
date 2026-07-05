namespace BoardGames.Abstractions;

public static class EnumerableExt
{
    public static Task<List<TResult>> ParallelSelectToList<T, TResult>(
        this IEnumerable<T> source,
        Func<T, CancellationToken, Task<TResult>> selector,
        CancellationToken cancellationToken = default)
        => source.ParallelSelectToList(selector, 128, cancellationToken);

    public static async Task<List<TResult>> ParallelSelectToList<T, TResult>(
        this IEnumerable<T> source,
        Func<T, CancellationToken, Task<TResult>> selector,
        int packSize,
        CancellationToken cancellationToken = default)
    {
        var result = new List<TResult>();
        foreach (var pack in source.Chunk(packSize)) {
            var tasks = pack.Select(i => selector.Invoke(i, cancellationToken));
            var results = await Task.WhenAll(tasks);
            result.AddRange(results);
        }
        return result;
    }
}
