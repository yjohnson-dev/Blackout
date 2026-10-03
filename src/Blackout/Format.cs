using System;

namespace Blackout;

internal static class Format
{
    public static string Duration(long milliseconds)
    {
        var span = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes}:{span.Seconds:00}";
    }

    /// <summary>A short, unit-bearing duration for the overlay reminder.</summary>
    public static string ReminderTime(long milliseconds)
    {
        var span = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}:{span.Minutes:00} hrs";

        return span.Seconds == 0
            ? $"{span.Minutes} min"
            : $"{span.Minutes}:{span.Seconds:00} min";
    }
}
