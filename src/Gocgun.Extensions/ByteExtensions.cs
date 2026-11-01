namespace Gocgun.Extensions;

/// <summary>Byte helpers for upload sniffing.</summary>
public static class ByteExtensions
{
    private const string HexDigits = "0123456789ABCDEF";

    /// <summary>Wraps the bytes in a memory stream.</summary>
    /// <param name="value">The bytes.</param>
    /// <returns>A readable stream.</returns>
    public static Stream ReadAsStream(this byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new MemoryStream(value, writable: false);
    }

    /// <summary>Hex prefix used to sniff a file header.</summary>
    /// <param name="value">The bytes.</param>
    /// <param name="byteCount">How many leading bytes to encode. Default is 20.</param>
    /// <returns>Upper-case hex, or empty.</returns>
    public static string ToHexPrefix(this byte[] value, int byteCount = 20)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(byteCount);
        var length = Math.Min(byteCount, value.Length);
        if (length == 0)
        {
            return string.Empty;
        }

        return string.Create(length * 2, (value, length), static (destination, state) =>
        {
            var hex = HexDigits.AsSpan();
            var source = state.value.AsSpan(0, state.length);
            for (var index = 0; index < source.Length; index++)
            {
                var current = source[index];
                destination[index * 2] = hex[current >> 4];
                destination[(index * 2) + 1] = hex[current & 0xF];
            }
        });
    }
}
