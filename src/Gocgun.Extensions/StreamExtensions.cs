using System.Buffers;

namespace Gocgun.Extensions;

/// <summary>Stream copies. <see cref="ToByteArray"/> leaves the stream open unless asked not to.</summary>
public static class StreamExtensions
{
    private const int CopyBufferSize = 65_536;

    /// <summary>Reads the stream as base64. An unreadable stream returns empty.</summary>
    /// <param name="source">The stream.</param>
    /// <param name="leaveOpen">When false, the stream is disposed.</param>
    /// <returns>The base64 text.</returns>
    public static string ToBase64String(this Stream source, bool leaveOpen = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        var bytes = source.ToByteArray(leaveOpen);
        return bytes.Length == 0 ? string.Empty : Convert.ToBase64String(bytes);
    }

    /// <summary>Copies the stream into an array. The caller stream stays open when <paramref name="leaveOpen"/> is true.</summary>
    /// <param name="stream">The stream.</param>
    /// <param name="leaveOpen">When false, the stream is disposed after the copy.</param>
    /// <returns>The bytes, or empty when the stream cannot be read.</returns>
    public static byte[] ToByteArray(this Stream stream, bool leaveOpen = true)
    {
        ArgumentNullException.ThrowIfNull(stream);
        try
        {
            if (!stream.CanRead)
            {
                return [];
            }

            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }

            var rented = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
            try
            {
                using var writer = new MemoryStream();
                int read;
                while ((read = stream.Read(rented, 0, rented.Length)) > 0)
                {
                    writer.Write(rented, 0, read);
                }

                return writer.ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
        finally
        {
            if (!leaveOpen)
            {
                stream.Dispose();
            }
        }
    }

    /// <summary>Copies <paramref name="stream"/> into a new memory stream positioned at zero.</summary>
    /// <param name="stream">The source. It is not disposed.</param>
    /// <param name="cancellationToken">Token that cancels the copy.</param>
    /// <returns>The copy.</returns>
    public static async Task<MemoryStream> ToMemoryStreamAsync(this Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var memory = new MemoryStream();
        try
        {
            var rented = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
            try
            {
                int read;
                while ((read = await stream.ReadAsync(rented.AsMemory(0, rented.Length), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await memory.WriteAsync(rented.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            memory.Position = 0;
            return memory;
        }
        catch
        {
            await memory.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
