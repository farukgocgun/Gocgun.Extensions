namespace Gocgun.Extensions;

/// <summary>GUID helpers.</summary>
public static class GuidExtensions
{
    /// <summary>True when the value is missing or <see cref="Guid.Empty"/>.</summary>
    /// <param name="value">The value.</param>
    /// <returns>True when there is no id.</returns>
    public static bool IsNullOrEmpty(this Guid? value) => !value.HasValue || value.Value == Guid.Empty;

    /// <summary>True when the value is <see cref="Guid.Empty"/>.</summary>
    /// <param name="value">The value.</param>
    /// <returns>True for the empty GUID.</returns>
    public static bool IsEmpty(this Guid value) => value == Guid.Empty;
}
