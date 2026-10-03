using System;

namespace Blackout;

/// <summary>One source of time. The 32-bit wrap of GetLastInputInfo is handled here only.</summary>
internal static class Clock
{
    public static long Now => Environment.TickCount64;

    public static long IdleMs => unchecked((uint)Environment.TickCount - Win32.LastInputTick());
}
