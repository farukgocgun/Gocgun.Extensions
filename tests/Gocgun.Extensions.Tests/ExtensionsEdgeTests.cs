using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace Gocgun.Extensions.Tests;

/// <summary>Text helpers not covered by <see cref="StringExtensionsTests"/>.</summary>
public sealed class StringExtensionsEdgeTests
{
    /// <summary>Whitespace counts as no value, and case-insensitive equality is ordinal.</summary>
    [Fact]
    public void HasValue_and_EqualsOrdinalIgnoreCase()
    {
        Assert.False(((string?)null).HasValue());
        Assert.False(" \t".HasValue());
        Assert.True("a".HasValue());
        Assert.True("ABC".EqualsOrdinalIgnoreCase("abc"));
        Assert.True(((string?)null).EqualsOrdinalIgnoreCase(null));
        Assert.False("a".EqualsOrdinalIgnoreCase(null));
    }

    /// <summary>Left and Right clamp to the length, take the absolute count and return empty for blank input.</summary>
    [Fact]
    public void Left_and_Right_clamp_and_use_absolute_length()
    {
        Assert.Equal("ab", "abcdef".Left(2));
        Assert.Equal("ab", "ab".Left(5));
        Assert.Equal("ab", "abc".Left(-2));
        Assert.Equal(string.Empty, "abc".Left(0));
        Assert.Equal(string.Empty, ((string?)null).Left(2));
        Assert.Equal(string.Empty, "   ".Left(1));
        Assert.Equal("ef", "abcdef".Right(2));
        Assert.Equal("ab", "ab".Right(5));
        Assert.Equal("c", "abc".Right(-1));
        Assert.Equal(string.Empty, ((string?)null).Right(3));
    }

    /// <summary>In ignores case by default and honors a supplied comparer.</summary>
    [Fact]
    public void In_defaults_to_ordinal_ignore_case()
    {
        string[] list = ["foo", "bar"];
        Assert.True("FOO".In(list));
        Assert.False("FOO".In(list, StringComparer.Ordinal));
        Assert.False("baz".In(list));
        Assert.False(((string?)null).In(list));
        Assert.False(" ".In([" "]));
        Assert.Throws<ArgumentNullException>(() => "foo".In(null!));
    }

    /// <summary>TruncateString cuts at the limit and validates its arguments.</summary>
    [Fact]
    public void TruncateString_cuts_and_validates()
    {
        Assert.Equal("abc", "abcdef".TruncateString(3));
        Assert.Equal("ab", "ab".TruncateString(5));
        Assert.Equal(string.Empty, "abc".TruncateString(0));
        Assert.Equal(string.Empty, string.Empty.TruncateString(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => "abc".TruncateString(-1));
        Assert.Throws<ArgumentNullException>(() => ((string)null!).TruncateString(1));
    }

    /// <summary>Only the words true and false parse; anything else returns the default.</summary>
    [Fact]
    public void ToBoolean_accepts_only_true_and_false_words()
    {
        Assert.True(" TRUE ".ToBoolean());
        Assert.False("false".ToBoolean());
        Assert.Null("yes".ToBoolean());
        Assert.Null("1".ToBoolean());
        Assert.True("yes".ToBoolean(defaultValue: true));
        Assert.False(((string?)null).ToBoolean(defaultValue: false));
        Assert.Null(string.Empty.ToBoolean());
    }

    /// <summary>Each word gets an upper first letter and lower rest, in the given culture.</summary>
    [Fact]
    public void ToPascalCaseSentence_normalizes_each_word()
    {
        var turkish = CultureInfo.GetCultureInfo("tr-TR");
        Assert.Equal("Hello World", "hELLO wORLD".ToPascalCaseSentence(CultureInfo.InvariantCulture));
        Assert.Equal("Hello Big World", "  hello   big  world ".ToPascalCaseSentence(CultureInfo.InvariantCulture));
        Assert.Equal("A", " a ".ToPascalCaseSentence(CultureInfo.InvariantCulture));
        Assert.Equal("İstanbul", "istanbul".ToPascalCaseSentence(turkish));
        Assert.Equal("Istanbul", "ıSTANBUL".ToPascalCaseSentence(turkish));
        Assert.Equal(string.Empty, ((string?)null).ToPascalCaseSentence(CultureInfo.InvariantCulture));
        Assert.Throws<ArgumentNullException>(() => "a".ToPascalCaseSentence(null!));
    }

    /// <summary>Mask works per word, clamps negative edges and keeps one letter when nothing would be hidden.</summary>
    [Fact]
    public void Mask_per_word_and_short_words()
    {
        var culture = CultureInfo.InvariantCulture;
        Assert.Equal("J**n D*e", "john doe".Mask(1, 1, capitalize: true, culture));
        Assert.Equal("a*c", "abc".Mask(1, 1, false, culture));
        Assert.Equal("a*", "ab".Mask(1, 1, false, culture));
        Assert.Equal("a*", "ab".Mask(2, 0, false, culture));
        Assert.Equal("ab****", "abcdef".Mask(2, -3, false, culture));
        Assert.Equal("****ef", "abcdef".Mask(-1, 2, false, culture));
        Assert.Equal(string.Empty, "abcdef".Mask(0, 0, false, culture));
        Assert.Equal("a**  b**", "abc  bcd".Mask(1, 0, false, culture));
        Assert.Throws<ArgumentNullException>(() => "a".Mask(1, 1, false, null!));
    }

    /// <summary>IsEmail trims and rejects display-name forms and malformed addresses.</summary>
    [Fact]
    public void IsEmail_rejects_display_names_and_malformed()
    {
        Assert.True(" a@b.co ".IsEmail());
        Assert.False("Ada <a@b.co>".IsEmail());
        Assert.False("a@@b.co".IsEmail());
        Assert.False(" ".IsEmail());
    }

    /// <summary>Turkish letters map to ASCII and separators become underscores.</summary>
    [Fact]
    public void CleanFileName_maps_turkish_letters_and_separators()
    {
        Assert.Equal("Cigdem_Odev_pdf", "Çiğdem Ödev.pdf".CleanFileName());
        Assert.Equal("a_b_c_d", "a:b?c'd".CleanFileName());
        Assert.Equal("ISIK_sogus", "IŞIK şoğuş".CleanFileName());
        Assert.NotEqual("   ".CleanFileName(), "   ".CleanFileName(), StringComparer.Ordinal);
    }

    /// <summary>Combining marks are removed while letters without a decomposition stay.</summary>
    [Fact]
    public void RemoveDiacritics_keeps_letters_without_decomposition()
    {
        Assert.Equal("Agac", "Ağaç".RemoveDiacritics());
        Assert.Equal("I", "İ".RemoveDiacritics());
        Assert.Equal("ı", "ı".RemoveDiacritics());
        Assert.Equal(string.Empty, string.Empty.RemoveDiacritics());
        Assert.Throws<ArgumentNullException>(() => ((string)null!).RemoveDiacritics());
    }
}

/// <summary>Date helpers not covered by <see cref="DateTimeExtensionsTests"/>.</summary>
public sealed class DateTimeExtensionsEdgeTests
{
    private static readonly TimeSpan LastTick = TimeOnly.MaxValue.ToTimeSpan();

    /// <summary>Month and year bounds keep the kind and end on the last tick of the day.</summary>
    [Fact]
    public void Month_and_year_bounds_keep_kind()
    {
        var value = new DateTime(2026, 2, 17, 13, 45, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), value.StartOfMonth());
        Assert.Equal(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc).Add(LastTick), value.EndOfMonth());
        Assert.Equal(new DateTime(2028, 2, 29, 0, 0, 0, DateTimeKind.Local).Add(LastTick), new DateTime(2028, 2, 3, 0, 0, 0, DateTimeKind.Local).EndOfMonth());
        Assert.Equal(DateTimeKind.Utc, value.EndOfMonth().Kind);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), value.StartOfYear());
        Assert.Equal(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc).Add(LastTick), value.EndOfYear());
        Assert.Equal(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 17).StartOfMonth());
        Assert.Equal(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1).StartOfMonth());
        Assert.Equal(new DateOnly(2028, 2, 29), new DateOnly(2028, 2, 3).EndOfMonth());
        Assert.Equal(new DateOnly(2026, 12, 31), new DateOnly(2026, 12, 1).EndOfMonth());
    }

    /// <summary>Week bounds work for any first day, including when the value is that day.</summary>
    [Fact]
    public void Week_bounds_for_any_first_day()
    {
        var thursday = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc), thursday.StartOfWeek(DayOfWeek.Monday));
        Assert.Equal(new DateTime(2026, 1, 18, 0, 0, 0, DateTimeKind.Utc).Add(LastTick), thursday.EndOfWeek(DayOfWeek.Monday));
        Assert.Equal(new DateTime(2026, 1, 11, 0, 0, 0, DateTimeKind.Utc), thursday.StartOfWeek(DayOfWeek.Sunday));
        Assert.Equal(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), thursday.StartOfWeek(DayOfWeek.Thursday));
        Assert.Equal(new DateTime(2026, 1, 9, 0, 0, 0, DateTimeKind.Utc), thursday.StartOfWeek(DayOfWeek.Friday));
    }

    /// <summary>Kind helpers keep the ticks, and the sentinel check matches only min and max.</summary>
    [Fact]
    public void Kind_and_sentinel_helpers()
    {
        var local = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Local);
        var utc = local.ToSpecifyKindAsUtc();
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(local.Ticks, utc.Ticks);
        Assert.True(DateTime.MinValue.IsMinOrMaxValue());
        Assert.True(DateTime.MaxValue.IsMinOrMaxValue());
        Assert.False(DateTime.MinValue.AddTicks(1).IsMinOrMaxValue());
    }

    /// <summary>Date and time-of-day rebuilds keep whole seconds only.</summary>
    [Fact]
    public void Rebuilds_drop_sub_second_precision()
    {
        var value = new DateTime(2026, 1, 15, 8, 9, 10, 500, DateTimeKind.Local);
        Assert.Equal(new DateTime(2027, 3, 4, 8, 9, 10, DateTimeKind.Utc), value.UpdateDateAs(new DateOnly(2027, 3, 4)));
        Assert.Equal(DateTimeKind.Local, value.UpdateDateAs(new DateOnly(2027, 3, 4), DateTimeKind.Local).Kind);
        var time = new TimeOnly(4, 5, 6, 789);
        var rebuilt = time.ToDateTimeUtcFromTimeOnly(new DateTime(2026, 6, 2, 15, 0, 0, DateTimeKind.Unspecified), DateTimeKind.Local);
        Assert.Equal(new DateTime(2026, 6, 2, 4, 5, 6, DateTimeKind.Local), rebuilt);
        Assert.Equal(DateTimeKind.Local, rebuilt.Kind);
        Assert.Null(((DateTime?)null).ToTimeOnlyFromDateTime());
        Assert.Equal(new TimeOnly(8, 9, 10, 500), ((DateTime?)value).ToTimeOnlyFromDateTime());
        Assert.Equal(new TimeOnly(8, 9, 10, 500), value.ToTimeOnlyFromDateTime());
    }

    /// <summary>The nearest occurrence may be yesterday or tomorrow, and a tie keeps today.</summary>
    [Fact]
    public void GetNearestDateTimeFromTimeOnly_picks_closest_day()
    {
        var justAfterMidnight = new DateTime(2026, 1, 15, 0, 10, 0, DateTimeKind.Utc);
        var lateEvening = new DateTime(2026, 1, 15, 23, 50, 0, DateTimeKind.Utc);
        var noon = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 1, 14, 23, 30, 0, DateTimeKind.Utc), new TimeOnly(23, 30).GetNearestDateTimeFromTimeOnly(justAfterMidnight));
        Assert.Equal(new DateTime(2026, 1, 16, 1, 0, 0, DateTimeKind.Utc), new TimeOnly(1, 0).GetNearestDateTimeFromTimeOnly(lateEvening));
        Assert.Equal(noon, new TimeOnly(12, 0).GetNearestDateTimeFromTimeOnly(noon));
        Assert.Equal(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc), new TimeOnly(0, 0).GetNearestDateTimeFromTimeOnly(noon));
        Assert.Equal(new DateTime(2025, 12, 31, 23, 50, 0, DateTimeKind.Utc), new TimeOnly(23, 50).GetNearestDateTimeFromTimeOnly(new DateTime(2026, 1, 1, 0, 10, 0, DateTimeKind.Utc)));
        Assert.Equal(DateTimeKind.Local, new TimeOnly(1, 0).GetNearestDateTimeFromTimeOnly(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local)).Kind);
    }

    /// <summary>An unspecified kind is read as UTC by FromUtc, and null arguments are rejected.</summary>
    [Fact]
    public void FromUtc_treats_unspecified_as_utc()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Minus5", TimeSpan.FromHours(-5), "Minus5", "Minus5");
        var unspecified = new DateTime(2026, 1, 1, 3, 0, 0, DateTimeKind.Unspecified);
        Assert.Equal(new DateTime(2025, 12, 31, 22, 0, 0, DateTimeKind.Unspecified), unspecified.FromUtc(zone));
        Assert.Equal(new DateOnly(2025, 12, 31), unspecified.ToDateInZone(zone));
        Assert.Throws<ArgumentNullException>(() => unspecified.FromUtc(null!));
        Assert.Throws<ArgumentNullException>(() => ((TimeProvider)null!).TodayInZone(zone));
        Assert.Throws<ArgumentNullException>(() => TimeProvider.System.TodayInZone(null!));
        Assert.Throws<ArgumentNullException>(() => new TimeOnly(1, 0).ToDateTimeUtcFromTimeOnly((TimeProvider)null!));
    }
}

/// <summary>Stream, byte and claim helpers not covered by <see cref="OtherExtensionsTests"/>.</summary>
public sealed class OtherExtensionsEdgeTests
{
    /// <summary>ReadAsStream wraps the bytes read-only.</summary>
    [Fact]
    public void ReadAsStream_is_read_only()
    {
        using var stream = Encoding.UTF8.GetBytes("abc").ReadAsStream();
        Assert.False(stream.CanWrite);
        Assert.Equal("abc", Encoding.UTF8.GetString(stream.ToByteArray()));
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(1));
        Assert.Throws<ArgumentNullException>(() => ((byte[])null!).ReadAsStream());
    }

    /// <summary>The hex prefix defaults to 20 bytes and is empty for an empty array.</summary>
    [Fact]
    public void ToHexPrefix_default_and_empty()
    {
        var bytes = Enumerable.Range(0, 25).Select(value => (byte)value).ToArray();
        Assert.Equal(40, bytes.ToHexPrefix().Length);
        Assert.StartsWith("000102", bytes.ToHexPrefix(), StringComparison.Ordinal);
        Assert.Equal("FF", new byte[] { 0xFF }.ToHexPrefix(5));
        Assert.Equal(string.Empty, Array.Empty<byte>().ToHexPrefix());
        Assert.Throws<ArgumentNullException>(() => ((byte[])null!).ToHexPrefix());
    }

    /// <summary>Non-memory streams are copied through the pooled buffer; unreadable streams give nothing.</summary>
    [Fact]
    public void ToByteArray_copies_large_non_memory_stream_and_skips_unreadable()
    {
        var payload = Enumerable.Range(0, 200_000).Select(value => (byte)(value % 251)).ToArray();
        using var buffered = new BufferedStream(new MemoryStream(payload));
        Assert.Equal(payload, buffered.ToByteArray());
        var closed = new MemoryStream(payload);
        closed.Dispose();
        Assert.Empty(closed.ToByteArray());
        Assert.Throws<ArgumentNullException>(() => ((Stream)null!).ToByteArray());
    }

    /// <summary>Base64 of an empty stream is empty; otherwise standard Base64.</summary>
    [Fact]
    public void ToBase64String_empty_and_value()
    {
        using var empty = new MemoryStream();
        using var value = new MemoryStream(Encoding.UTF8.GetBytes("hi"));
        Assert.Equal(string.Empty, empty.ToBase64String());
        Assert.Equal("aGk=", value.ToBase64String());
        Assert.True(value.CanRead);
        Assert.Equal("aGk=", value.ToBase64String(leaveOpen: false));
        Assert.False(value.CanRead);
    }

    /// <summary>The async copy handles input larger than the buffer and honors a canceled token.</summary>
    [Fact]
    public async Task ToMemoryStreamAsync_large_and_canceled()
    {
        var payload = Enumerable.Range(0, 150_000).Select(value => (byte)(value % 13)).ToArray();
        await using var source = new BufferedStream(new MemoryStream(payload));
        await using var copy = await source.ToMemoryStreamAsync(TestContext.Current.CancellationToken);
        Assert.Equal(payload, copy.ToArray());
        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();
        await using var again = new MemoryStream(payload);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => again.ToMemoryStreamAsync(canceled.Token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ((Stream)null!).ToMemoryStreamAsync(CancellationToken.None));
    }

    /// <summary>String claims read the first match; a null principal or bad GUID returns null.</summary>
    [Fact]
    public void GetStringClaim_first_match_and_nulls()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("role", "a"), new Claim("role", "b"), new Claim("sub", "not-a-guid")]));
        Assert.Equal("a", principal.GetStringClaim("role"));
        Assert.Null(principal.GetStringClaim("missing"));
        Assert.Null(((ClaimsPrincipal?)null).GetStringClaim("role"));
        Assert.Null(principal.GetGuidClaim("sub"));
        Assert.Null(((ClaimsPrincipal?)null).GetGuidClaim("sub"));
        Assert.Throws<ArgumentException>(() => principal.GetStringClaim(string.Empty));
    }

    /// <summary>GUID helpers treat only Guid.Empty and null as empty.</summary>
    [Fact]
    public void Guid_helpers()
    {
        Assert.False(Guid.NewGuid().IsEmpty());
        Assert.True(((Guid?)Guid.Empty).IsNullOrEmpty());
        Assert.False(((Guid?)Guid.NewGuid()).IsNullOrEmpty());
    }
}
