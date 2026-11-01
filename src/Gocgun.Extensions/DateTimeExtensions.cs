namespace Gocgun.Extensions;

/// <summary>Date helpers. Zone conversions take a time zone. Clock reads take a <see cref="TimeProvider"/>.</summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a local wall time in <paramref name="zone"/> to UTC.
    /// A wall time inside a spring-forward gap does not exist and throws; use the overload with <see cref="DstGapPolicy"/> to shift it instead.
    /// An ambiguous wall time (fall-back overlap) resolves to the standard-time instant, which is the later of the two.
    /// </summary>
    /// <param name="local">Wall time. Its kind is ignored.</param>
    /// <param name="zone">Zone of the wall time.</param>
    /// <returns>The UTC instant.</returns>
    /// <exception cref="ArgumentException">The wall time falls in a daylight-saving gap of <paramref name="zone"/>.</exception>
    public static DateTime ToUtc(this DateTime local, TimeZoneInfo zone) => local.ToUtc(zone, DstGapPolicy.Throw);

    /// <summary>
    /// Converts a local wall time in <paramref name="zone"/> to UTC and decides what happens in a daylight-saving gap.
    /// An ambiguous wall time (fall-back overlap) resolves to the standard-time instant, which is the later of the two.
    /// </summary>
    /// <param name="local">Wall time. Its kind is ignored.</param>
    /// <param name="zone">Zone of the wall time.</param>
    /// <param name="gapPolicy">Behavior for a wall time that does not exist.</param>
    /// <returns>The UTC instant.</returns>
    /// <exception cref="ArgumentException">The wall time falls in a gap and <paramref name="gapPolicy"/> is <see cref="DstGapPolicy.Throw"/>.</exception>
    public static DateTime ToUtc(this DateTime local, TimeZoneInfo zone, DstGapPolicy gapPolicy)
    {
        ArgumentNullException.ThrowIfNull(zone);
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (gapPolicy == DstGapPolicy.ShiftForward && zone.IsInvalidTime(unspecified))
        {
            var gap = zone.GetUtcOffset(unspecified.AddDays(1)) - zone.GetUtcOffset(unspecified.AddDays(-1));
            unspecified = unspecified.Add(gap);
        }

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, zone);
    }

    /// <summary>Converts a UTC instant to wall time in <paramref name="zone"/>.</summary>
    /// <param name="utc">UTC instant. Unspecified values are treated as UTC.</param>
    /// <param name="zone">Target zone.</param>
    /// <returns>The wall time.</returns>
    /// <exception cref="ArgumentException"><paramref name="utc"/> has <see cref="DateTimeKind.Local"/>, which is neither UTC nor unspecified.</exception>
    public static DateTime FromUtc(this DateTime utc, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        if (utc.Kind == DateTimeKind.Local)
        {
            throw new ArgumentException("A local DateTime is not a UTC instant. Convert it with ToUniversalTime() first.", nameof(utc));
        }

        var instant = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(instant, zone);
    }

    /// <summary>Calendar date of a UTC instant in <paramref name="zone"/>.</summary>
    /// <param name="utc">UTC instant.</param>
    /// <param name="zone">Target zone.</param>
    /// <returns>The local date.</returns>
    public static DateOnly ToDateInZone(this DateTime utc, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(utc.FromUtc(zone));

    /// <summary>Today's UTC date from <paramref name="time"/>, then the wall date in <paramref name="zone"/>.</summary>
    /// <param name="time">Clock.</param>
    /// <param name="zone">Target zone.</param>
    /// <returns>The local date.</returns>
    public static DateOnly TodayInZone(this TimeProvider time, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(zone);
        return time.GetUtcNow().UtcDateTime.ToDateInZone(zone);
    }

    /// <summary>Start of the week that contains <paramref name="value"/>. The result keeps <paramref name="value"/>'s kind.</summary>
    /// <param name="value">The instant.</param>
    /// <param name="firstDay">First day of the week.</param>
    /// <returns>Midnight at the start of that week.</returns>
    public static DateTime StartOfWeek(this DateTime value, DayOfWeek firstDay)
    {
        var diff = ((int)value.DayOfWeek - (int)firstDay + 7) % 7;
        return DateTime.SpecifyKind(value.AddDays(-diff).Date, value.Kind);
    }

    /// <summary>End of the week that contains <paramref name="value"/>. The result keeps the input kind.</summary>
    /// <param name="value">The instant.</param>
    /// <param name="firstDay">First day of the week.</param>
    /// <returns>The last representable time on the last day of that week.</returns>
    public static DateTime EndOfWeek(this DateTime value, DayOfWeek firstDay) =>
        DateTime.SpecifyKind(value.StartOfWeek(firstDay).AddDays(6).Add(TimeOnly.MaxValue.ToTimeSpan()), value.Kind);

    /// <summary>Midnight on the first day of the month. Keeps the input kind.</summary>
    /// <param name="value">The instant.</param>
    /// <returns>The start of the month.</returns>
    public static DateTime StartOfMonth(this DateTime value) =>
        new(value.Year, value.Month, 1, 0, 0, 0, value.Kind);

    /// <summary>First day of the month.</summary>
    /// <param name="value">The date.</param>
    /// <returns>The first of that month.</returns>
    public static DateOnly StartOfMonth(this DateOnly value) =>
        value.Day == 1 ? value : new DateOnly(value.Year, value.Month, 1);

    /// <summary>Last day of the month.</summary>
    /// <param name="value">The date.</param>
    /// <returns>The last day of that month.</returns>
    public static DateOnly EndOfMonth(this DateOnly value) =>
        new(value.Year, value.Month, DateTime.DaysInMonth(value.Year, value.Month));

    /// <summary>Last representable time on the last day of the month. Keeps the input kind.</summary>
    /// <param name="value">The instant.</param>
    /// <returns>The end of the month.</returns>
    public static DateTime EndOfMonth(this DateTime value) =>
        new DateTime(value.Year, value.Month, DateTime.DaysInMonth(value.Year, value.Month), 0, 0, 0, value.Kind)
            .Add(TimeOnly.MaxValue.ToTimeSpan());

    /// <summary>Midnight on 1 January. Keeps the input kind.</summary>
    /// <param name="value">The instant.</param>
    /// <returns>The start of the year.</returns>
    public static DateTime StartOfYear(this DateTime value) =>
        new(value.Year, 1, 1, 0, 0, 0, value.Kind);

    /// <summary>Last representable time on 31 December. Keeps the input kind.</summary>
    /// <param name="value">The instant.</param>
    /// <returns>The end of the year.</returns>
    public static DateTime EndOfYear(this DateTime value) =>
        new DateTime(value.Year, 12, 31, 0, 0, 0, value.Kind).Add(TimeOnly.MaxValue.ToTimeSpan());

    /// <summary>Same ticks with <see cref="DateTimeKind.Utc"/>.</summary>
    /// <param name="value">The instant.</param>
    /// <returns>The value marked UTC.</returns>
    public static DateTime ToSpecifyKindAsUtc(this DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc);

    /// <summary>True for <see cref="DateTime.MinValue"/> or <see cref="DateTime.MaxValue"/>.</summary>
    /// <param name="value">The instant.</param>
    /// <returns>True at either sentinel.</returns>
    public static bool IsMinOrMaxValue(this DateTime value) =>
        value == DateTime.MinValue || value == DateTime.MaxValue;

    /// <summary>Replaces the date and sets the kind.</summary>
    /// <param name="value">The time of day source.</param>
    /// <param name="date">The new date.</param>
    /// <param name="kind">Kind of the result.</param>
    /// <returns>The combined value.</returns>
    public static DateTime UpdateDateAs(this DateTime value, DateOnly date, DateTimeKind kind = DateTimeKind.Utc) =>
        new(date.Year, date.Month, date.Day, value.Hour, value.Minute, value.Second, kind);

    /// <summary>Time of day, or null.</summary>
    /// <param name="value">The instant.</param>
    /// <returns>The time, or null.</returns>
    public static TimeOnly? ToTimeOnlyFromDateTime(this DateTime? value) =>
        value.HasValue ? TimeOnly.FromDateTime(value.Value) : null;

    /// <summary>Time of day.</summary>
    /// <param name="value">The instant.</param>
    /// <returns>The time.</returns>
    public static TimeOnly ToTimeOnlyFromDateTime(this DateTime value) => TimeOnly.FromDateTime(value);

    /// <summary>UTC date from <paramref name="time"/> plus <paramref name="value"/>.</summary>
    /// <param name="value">The time of day.</param>
    /// <param name="time">Clock that supplies today's UTC date.</param>
    /// <returns>A UTC instant.</returns>
    public static DateTime ToDateTimeUtcFromTimeOnly(this TimeOnly value, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        var date = time.GetUtcNow().UtcDateTime.Date;
        return DateTime.SpecifyKind(date.Add(value.ToTimeSpan()), DateTimeKind.Utc);
    }

    /// <summary>Combines <paramref name="date"/>'s calendar day with this time.</summary>
    /// <param name="value">The time of day.</param>
    /// <param name="date">The calendar day.</param>
    /// <param name="kind">Kind of the result.</param>
    /// <returns>The combined value.</returns>
    public static DateTime ToDateTimeUtcFromTimeOnly(this TimeOnly value, DateTime date, DateTimeKind kind = DateTimeKind.Utc) =>
        new(date.Year, date.Month, date.Day, value.Hour, value.Minute, value.Second, kind);

    /// <summary>Yesterday, today, or tomorrow at this time, whichever is nearest <paramref name="reference"/>.</summary>
    /// <param name="value">The time of day.</param>
    /// <param name="reference">The reference instant. Its kind is kept.</param>
    /// <returns>The nearest instant.</returns>
    public static DateTime GetNearestDateTimeFromTimeOnly(this TimeOnly value, DateTime reference)
    {
        var today = reference.Date;
        var candidates = new[]
        {
            new DateTime(today.Year, today.Month, today.Day, value.Hour, value.Minute, value.Second, reference.Kind),
            new DateTime(today.AddDays(-1).Year, today.AddDays(-1).Month, today.AddDays(-1).Day, value.Hour, value.Minute, value.Second, reference.Kind),
            new DateTime(today.AddDays(1).Year, today.AddDays(1).Month, today.AddDays(1).Day, value.Hour, value.Minute, value.Second, reference.Kind),
        };
        var best = candidates[0];
        var bestDiff = Math.Abs((best - reference).Ticks);
        for (var index = 1; index < candidates.Length; index++)
        {
            var diff = Math.Abs((candidates[index] - reference).Ticks);
            if (diff < bestDiff)
            {
                best = candidates[index];
                bestDiff = diff;
            }
        }

        return best;
    }
}
