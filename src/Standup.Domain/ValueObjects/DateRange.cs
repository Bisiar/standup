namespace Standup.Domain.ValueObjects;

/// <summary>
/// Represents a date/time range for standup data queries
/// </summary>
public record DateRange(DateTimeOffset Start, DateTimeOffset End)
{
    public static DateRange LastWorkingDay(string timeZone = "America/Denver", bool skipWeekends = true)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var today = now.Date;

        var previousDay = today.AddDays(-1);

        if (skipWeekends)
        {
            // If today is Monday, go back to Friday
            if (today.DayOfWeek == DayOfWeek.Monday)
            {
                previousDay = today.AddDays(-3);
            }
            // If today is Sunday, go back to Friday
            else if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                previousDay = today.AddDays(-2);
            }
            // If today is Saturday, go back to Friday
            else if (today.DayOfWeek == DayOfWeek.Saturday)
            {
                previousDay = today.AddDays(-1);
            }
        }

        var start = new DateTimeOffset(previousDay, TimeSpan.Zero);
        var end = new DateTimeOffset(today, TimeSpan.Zero);

        return new DateRange(
            TimeZoneInfo.ConvertTimeToUtc(start.DateTime, tz),
            TimeZoneInfo.ConvertTimeToUtc(end.DateTime, tz));
    }

    public static DateRange LastNHours(int hours)
    {
        var now = DateTimeOffset.UtcNow;
        return new DateRange(now.AddHours(-hours), now);
    }

    public TimeSpan Duration => End - Start;
}
