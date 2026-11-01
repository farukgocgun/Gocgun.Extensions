namespace Gocgun.Extensions;

/// <summary>What <see cref="DateTimeExtensions.ToUtc(DateTime, TimeZoneInfo, DstGapPolicy)"/> does with a wall time that does not exist.</summary>
public enum DstGapPolicy
{
    /// <summary>Throw <see cref="ArgumentException"/>, like <see cref="TimeZoneInfo.ConvertTimeToUtc(DateTime, TimeZoneInfo)"/>.</summary>
    Throw = 0,

    /// <summary>Move the wall time forward by the length of the gap, so 02:30 in a 02:00 to 03:00 gap becomes 03:30.</summary>
    ShiftForward = 1,
}
