using System;

public static class EpochUtils
{
    /// <summary>
    /// Converts a Unix epoch timestamp (in seconds) to a DateTime.
    /// </summary>
    public static DateTime EpochToDateTime(long epochSeconds)
    {
        return DateTimeOffset.FromUnixTimeSeconds(epochSeconds).DateTime;
    }

    /// <summary>
    /// Converts a DateTime to a Unix epoch timestamp (in seconds).
    /// </summary>
    public static long DateTimeToEpoch(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Converts a Unix epoch timestamp (in milliseconds) to a DateTime.
    /// </summary>
    public static DateTime EpochMillisToDateTime(long epochMilliseconds)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(epochMilliseconds).DateTime;
    }

    /// <summary>
    /// Converts a DateTime to a Unix epoch timestamp (in milliseconds).
    /// </summary>
    public static long DateTimeToEpochMillis(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Gets the current Unix epoch timestamp (in seconds).
    /// </summary>
    public static long GetCurrentEpoch()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Gets the current Unix epoch timestamp (in milliseconds).
    /// </summary>
    public static long GetCurrentEpochMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Formats a DateTime object to a human-readable string.
    /// </summary>
    public static string FormatDateTime(DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss")
    {
        return dateTime.ToString(format);
    }

    /// <summary>
    /// Adds a number of seconds to a Unix epoch timestamp.
    /// </summary>
    public static long AddSecondsToEpoch(long epochSeconds, int secondsToAdd)
    {
        return epochSeconds + secondsToAdd;
    }

    /// <summary>
    /// Calculates the difference in seconds between two epoch timestamps.
    /// </summary>
    public static long GetEpochDifference(long epoch1, long epoch2)
    {
        return Math.Abs(epoch1 - epoch2);
    }
}
