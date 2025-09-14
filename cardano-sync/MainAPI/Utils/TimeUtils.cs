namespace MainAPI.Utils;

/// <summary>
/// Utility class for time-related calculations
/// </summary>
public static class TimeUtils
{
    /// <summary>
    /// Calculate the number of seconds remaining until the end of the current day
    /// </summary>
    /// <returns>Seconds remaining until midnight UTC</returns>
    public static int GetSecondsUntilEndOfDay()
    {
        var now = DateTime.UtcNow;
        var endOfDay = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            23,
            59,
            59,
            999,
            DateTimeKind.Utc
        );

        var diffSeconds = (int)Math.Ceiling((endOfDay - now).TotalSeconds);
        return diffSeconds;
    }

    /// <summary>
    /// Calculate the number of seconds remaining until the end of the current month
    /// </summary>
    /// <returns>Seconds remaining until the end of the month UTC</returns>
    public static int GetSecondsUntilEndOfMonth()
    {
        var now = DateTime.UtcNow;
        var endOfMonth = new DateTime(
            now.Year,
            now.Month,
            DateTime.DaysInMonth(now.Year, now.Month),
            23,
            59,
            59,
            999,
            DateTimeKind.Utc
        );

        var diffSeconds = (int)Math.Ceiling((endOfMonth - now).TotalSeconds);
        return diffSeconds;
    }

    /// <summary>
    /// Calculate the number of seconds from now to a specified number of hours
    /// </summary>
    /// <param name="hours">The number of hours to convert to seconds</param>
    /// <returns>Seconds equivalent to the specified hours</returns>
    public static int GetSecondsFromHours(int hours)
    {
        return hours * 60 * 60;
    }

    /// <summary>
    /// Calculate the number of minutes remaining until the end of the current day
    /// </summary>
    /// <returns>Minutes remaining until midnight UTC</returns>
    public static int GetMinutesUntilEndOfDay()
    {
        var now = DateTime.UtcNow;
        var endOfDay = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            23,
            59,
            59,
            999,
            DateTimeKind.Utc
        );

        var diffMinutes = (int)Math.Ceiling((endOfDay - now).TotalMinutes);
        return diffMinutes;
    }

    /// <summary>
    /// Calculate the number of minutes remaining until the end of the current month
    /// </summary>
    /// <param name="roundUp">Whether to round up the result (default: true)</param>
    /// <returns>Minutes remaining until the end of the month UTC</returns>
    public static double GetMinutesUntilEndOfMonth(bool roundUp = true)
    {
        var now = DateTime.UtcNow;
        var endOfMonth = new DateTime(
            now.Year,
            now.Month,
            DateTime.DaysInMonth(now.Year, now.Month),
            23,
            59,
            59,
            999,
            DateTimeKind.Utc
        );

        var diffMinutes = (endOfMonth - now).TotalMinutes;

        // Return rounded-up minutes by default, or exact if roundUp is false
        return roundUp ? Math.Ceiling(diffMinutes) : diffMinutes;
    }

    /// <summary>
    /// Get the end of the current day as DateTime
    /// </summary>
    /// <returns>End of current day in UTC</returns>
    public static DateTime GetEndOfDay()
    {
        var now = DateTime.UtcNow;
        return new DateTime(
            now.Year,
            now.Month,
            now.Day,
            23,
            59,
            59,
            999,
            DateTimeKind.Utc
        );
    }

    /// <summary>
    /// Get the end of the current month as DateTime
    /// </summary>
    /// <returns>End of current month in UTC</returns>
    public static DateTime GetEndOfMonth()
    {
        var now = DateTime.UtcNow;
        return new DateTime(
            now.Year,
            now.Month,
            DateTime.DaysInMonth(now.Year, now.Month),
            23,
            59,
            59,
            999,
            DateTimeKind.Utc
        );
    }

    /// <summary>
    /// Calculate the number of seconds until a specific DateTime
    /// </summary>
    /// <param name="targetDateTime">The target DateTime to calculate seconds until</param>
    /// <returns>Seconds remaining until the target DateTime</returns>
    public static int GetSecondsUntil(DateTime targetDateTime)
    {
        var now = DateTime.UtcNow;
        var diffSeconds = (int)Math.Ceiling((targetDateTime - now).TotalSeconds);
        return Math.Max(0, diffSeconds); // Ensure non-negative
    }

    /// <summary>
    /// Calculate the number of minutes until a specific DateTime
    /// </summary>
    /// <param name="targetDateTime">The target DateTime to calculate minutes until</param>
    /// <param name="roundUp">Whether to round up the result (default: true)</param>
    /// <returns>Minutes remaining until the target DateTime</returns>
    public static double GetMinutesUntil(DateTime targetDateTime, bool roundUp = true)
    {
        var now = DateTime.UtcNow;
        var diffMinutes = (targetDateTime - now).TotalMinutes;

        if (diffMinutes < 0)
            return 0;

        return roundUp ? Math.Ceiling(diffMinutes) : diffMinutes;
    }

    /// <summary>
    /// Get a DateTime representing the start of the current day
    /// </summary>
    /// <returns>Start of current day in UTC</returns>
    public static DateTime GetStartOfDay()
    {
        var now = DateTime.UtcNow;
        return new DateTime(
            now.Year,
            now.Month,
            now.Day,
            0,
            0,
            0,
            0,
            DateTimeKind.Utc
        );
    }

    /// <summary>
    /// Get a DateTime representing the start of the current month
    /// </summary>
    /// <returns>Start of current month in UTC</returns>
    public static DateTime GetStartOfMonth()
    {
        var now = DateTime.UtcNow;
        return new DateTime(
            now.Year,
            now.Month,
            1,
            0,
            0,
            0,
            0,
            DateTimeKind.Utc
        );
    }

    /// <summary>
    /// Check if a DateTime is today
    /// </summary>
    /// <param name="dateTime">The DateTime to check</param>
    /// <returns>True if the DateTime is today, false otherwise</returns>
    public static bool IsToday(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        return dateTime.Date == now.Date;
    }

    /// <summary>
    /// Check if a DateTime is in the current month
    /// </summary>
    /// <param name="dateTime">The DateTime to check</param>
    /// <returns>True if the DateTime is in the current month, false otherwise</returns>
    public static bool IsCurrentMonth(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        return dateTime.Year == now.Year && dateTime.Month == now.Month;
    }

    /// <summary>
    /// Format a TimeSpan to a human-readable string
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to format</param>
    /// <returns>Human-readable string representation</returns>
    public static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
        {
            return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        }
        else if (timeSpan.TotalHours >= 1)
        {
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
        else
        {
            return $"{timeSpan.Seconds}s";
        }
    }

    /// <summary>
    /// Get a human-readable string for the time remaining until end of day
    /// </summary>
    /// <returns>Human-readable string for time remaining until end of day</returns>
    public static string GetTimeRemainingUntilEndOfDay()
    {
        var now = DateTime.UtcNow;
        var endOfDay = GetEndOfDay();
        var timeSpan = endOfDay - now;
        return FormatTimeSpan(timeSpan);
    }

    /// <summary>
    /// Get a human-readable string for the time remaining until end of month
    /// </summary>
    /// <returns>Human-readable string for time remaining until end of month</returns>
    public static string GetTimeRemainingUntilEndOfMonth()
    {
        var now = DateTime.UtcNow;
        var endOfMonth = GetEndOfMonth();
        var timeSpan = endOfMonth - now;
        return FormatTimeSpan(timeSpan);
    }
}