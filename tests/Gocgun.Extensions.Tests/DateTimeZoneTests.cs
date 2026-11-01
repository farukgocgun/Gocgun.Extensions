namespace Gocgun.Extensions.Tests;

/// <summary>Daylight-saving behavior of <see cref="DateTimeExtensions"/> zone conversions.</summary>
public sealed class DateTimeZoneTests
{
    private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    /// <summary>A wall time in the spring-forward gap throws by default.</summary>
    [Fact]
    public void ToUtc_in_dst_gap_throws()
    {
        var gap = new DateTime(2026, 3, 29, 2, 30, 0, DateTimeKind.Unspecified);
        Assert.Throws<ArgumentException>(() => gap.ToUtc(Berlin));
        Assert.Throws<ArgumentException>(() => gap.ToUtc(Berlin, DstGapPolicy.Throw));
    }

    /// <summary>ShiftForward moves a gap time past the gap by its length.</summary>
    [Fact]
    public void ToUtc_in_dst_gap_can_shift_forward()
    {
        var gap = new DateTime(2026, 3, 29, 2, 30, 0, DateTimeKind.Unspecified);
        var utc = gap.ToUtc(Berlin, DstGapPolicy.ShiftForward);
        Assert.Equal(new DateTime(2026, 3, 29, 1, 30, 0, DateTimeKind.Utc), utc);
        Assert.Equal(new DateTime(2026, 3, 29, 3, 30, 0, DateTimeKind.Unspecified), utc.FromUtc(Berlin));
    }

    /// <summary>ShiftForward leaves valid times alone.</summary>
    [Fact]
    public void ToUtc_shift_policy_keeps_valid_times()
    {
        var valid = new DateTime(2026, 3, 29, 3, 30, 0, DateTimeKind.Unspecified);
        Assert.Equal(valid.ToUtc(Berlin), valid.ToUtc(Berlin, DstGapPolicy.ShiftForward));
    }

    /// <summary>An ambiguous wall time resolves to the standard-time (later) instant.</summary>
    [Fact]
    public void ToUtc_ambiguous_time_uses_standard_offset()
    {
        var ambiguous = new DateTime(2026, 10, 25, 2, 30, 0, DateTimeKind.Unspecified);
        Assert.Equal(new DateTime(2026, 10, 25, 1, 30, 0, DateTimeKind.Utc), ambiguous.ToUtc(Berlin));
    }

    /// <summary>A local-kind value is not a UTC instant.</summary>
    [Fact]
    public void FromUtc_rejects_local_kind()
    {
        Assert.Throws<ArgumentException>(() => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local).FromUtc(Berlin));
    }
}
