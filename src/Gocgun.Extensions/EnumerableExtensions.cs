namespace Gocgun.Extensions;

/// <summary>Sequence helpers.</summary>
public static class EnumerableExtensions
{
    /// <summary>True when the sequence is null or has no items.</summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="source">The sequence.</param>
    /// <returns>True when there is nothing to enumerate.</returns>
    public static bool None<T>(this IEnumerable<T>? source) => source?.Any() != true;
}
