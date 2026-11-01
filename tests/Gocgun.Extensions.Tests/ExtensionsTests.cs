using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace Gocgun.Extensions.Tests;

/// <summary>Text helpers.</summary>
public sealed class StringExtensionsTests
{
    /// <summary>Digits at the minimum length pass, and a shorter string fails.</summary>
    [Fact]
    public void IsNumeric_uses_minimum_length()
    {
        Assert.False("12".IsNumeric(3));
        Assert.True("123".IsNumeric(3));
        Assert.False("12a".IsNumeric(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => "1".IsNumeric(-1));
    }

    /// <summary>A simple address passes and a bare word does not.</summary>
    [Fact]
    public void IsEmail_round_trips()
    {
        Assert.True("a@b.co".IsEmail());
        Assert.False("not-an-email".IsEmail());
        Assert.False(((string?)null).IsEmail());
    }

    /// <summary>Turkish casing follows the culture.</summary>
    [Fact]
    public void Capitalize_uses_culture()
    {
        var turkish = CultureInfo.GetCultureInfo("tr-TR");
        Assert.Equal("İstanbul", "istanbul".Capitalize(turkish));
        Assert.Equal("Istanbul", "istanbul".Capitalize(CultureInfo.InvariantCulture));
    }

    /// <summary>Masking keeps the requested edges.</summary>
    [Fact]
    public void Mask_keeps_edges()
    {
        Assert.Equal("ab***f", "abcdef".Mask(2, 1, capitalize: false, CultureInfo.InvariantCulture));
        Assert.Equal(string.Empty, ((string?)null).Mask(2, 0, false, CultureInfo.InvariantCulture));
    }

    /// <summary>Diacritics are removed, including a long input.</summary>
    [Fact]
    public void RemoveDiacritics_strips_marks_on_long_input()
    {
        Assert.Equal("e", "é".RemoveDiacritics());
        var input = new string('é', 20_000);
        var output = input.RemoveDiacritics();
        Assert.Equal(20_000, output.Length);
        Assert.Equal('e', output[0]);
    }

    /// <summary>Empty names become a GUID. Traversal and odd Unicode do not survive.</summary>
    [Fact]
    public void CleanFileName_blocks_traversal_and_unicode_tricks()
    {
        Assert.True(Guid.TryParse("   ".CleanFileName(), out _));
        var traversal = "../..\\etc/passwd".CleanFileName();
        Assert.DoesNotContain("..", traversal, StringComparison.Ordinal);
        Assert.DoesNotContain("/", traversal, StringComparison.Ordinal);
        Assert.DoesNotContain("\\", traversal, StringComparison.Ordinal);
        var spoofed = "invoice\u202Egpj.exe\0".CleanFileName();
        Assert.DoesNotContain("\u202E", spoofed, StringComparison.Ordinal);
        Assert.DoesNotContain("\0", spoofed, StringComparison.Ordinal);
        Assert.Equal("i", "ı".CleanFileName());
    }
}

/// <summary>Date helpers.</summary>
public sealed class DateTimeExtensionsTests
{
    /// <summary>Zone conversion and the clock date use the supplied zone.</summary>
    [Fact]
    public void Zone_conversion_uses_the_parameter()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Plus3", TimeSpan.FromHours(3), "Plus3", "Plus3");
        var utc = new DateTime(2026, 1, 15, 22, 0, 0, DateTimeKind.Utc);
        var local = utc.FromUtc(zone);
        Assert.Equal(16, local.Day);
        Assert.Equal(1, local.Hour);
        Assert.Equal(utc, local.ToUtc(zone));
        Assert.Equal(new DateOnly(2026, 1, 16), utc.ToDateInZone(zone));
        var clock = new FixedClock(new DateTimeOffset(2026, 1, 15, 22, 0, 0, TimeSpan.Zero));
        Assert.Equal(new DateOnly(2026, 1, 16), clock.TodayInZone(zone));
    }

    /// <summary>Week bounds keep the input kind.</summary>
    [Fact]
    public void StartOfWeek_keeps_kind()
    {
        var value = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Unspecified);
        var start = value.StartOfWeek(DayOfWeek.Monday);
        Assert.Equal(DayOfWeek.Monday, start.DayOfWeek);
        Assert.Equal(DateTimeKind.Unspecified, start.Kind);
        Assert.Equal(DateTimeKind.Unspecified, value.EndOfWeek(DayOfWeek.Monday).Kind);
    }

    /// <summary>A null zone is rejected.</summary>
    [Fact]
    public void Null_zone_throws() =>
        Assert.Throws<ArgumentNullException>(() => DateTime.UtcNow.ToUtc(null!));

    /// <summary>The time-only helper reads the clock.</summary>
    [Fact]
    public void TimeOnly_uses_time_provider()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero));
        var instant = new TimeOnly(4, 5, 6).ToDateTimeUtcFromTimeOnly(clock);
        Assert.Equal(new DateTime(2026, 6, 2, 4, 5, 6, DateTimeKind.Utc), instant);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

/// <summary>Streams, bytes, claims, and expressions.</summary>
public sealed class OtherExtensionsTests
{
    /// <summary>leaveOpen false disposes the caller stream.</summary>
    [Fact]
    public void ToByteArray_leaveOpen_false_disposes()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("hi"));
        var bytes = stream.ToByteArray(leaveOpen: false);
        Assert.Equal("hi", Encoding.UTF8.GetString(bytes));
        Assert.False(stream.CanRead);
    }

    /// <summary>The default leaves the stream open.</summary>
    [Fact]
    public void ToByteArray_leaves_stream_open()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("hi"));
        _ = stream.ToByteArray();
        Assert.True(stream.CanRead);
    }

    /// <summary>The hex prefix is bounded.</summary>
    [Fact]
    public void ToHexPrefix_limits_bytes()
    {
        Assert.Equal("0A0B", new byte[] { 0x0A, 0x0B, 0x0C }.ToHexPrefix(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new byte[] { 1 }.ToHexPrefix(0));
    }

    /// <summary>Copy honors cancellation and returns a rewound stream.</summary>
    [Fact]
    public async Task ToMemoryStreamAsync_copies()
    {
        await using var source = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
        await using var copy = await source.ToMemoryStreamAsync(CancellationToken.None);
        Assert.Equal(0, copy.Position);
        Assert.Equal(3, copy.Length);
    }

    /// <summary>The claim type is a parameter.</summary>
    [Fact]
    public void GetGuidClaim_reads_the_named_claim()
    {
        var id = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id.ToString("D"))]));
        Assert.Equal(id, principal.GetGuidClaim("sub"));
        Assert.Null(principal.GetGuidClaim(ClaimTypes.NameIdentifier));
        Assert.Throws<ArgumentException>(() => principal.GetGuidClaim(" "));
    }

    /// <summary>AndAlso combines two predicates.</summary>
    [Fact]
    public void AndAlso_combines_predicates()
    {
        System.Linq.Expressions.Expression<Func<int, bool>> left = value => value > 1;
        System.Linq.Expressions.Expression<Func<int, bool>> right = value => value < 5;
        var combined = left.AndAlso(right).Compile();
        Assert.True(combined(3));
        Assert.False(combined(1));
        Assert.Throws<ArgumentNullException>(() => left.AndAlso(null!));
    }

    /// <summary>Empty sequences and empty GUIDs are recognized.</summary>
    [Fact]
    public void None_and_empty_guid()
    {
        Assert.True(((int[]?)null).None());
        Assert.True(Array.Empty<int>().None());
        IEnumerable<int> oneItem = [1];
        Assert.False(oneItem.None());
        Assert.True(Guid.Empty.IsEmpty());
        Assert.True(((Guid?)null).IsNullOrEmpty());
    }
}
