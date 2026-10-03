using System;
using System.Linq;

namespace Blackout;

/// <summary>Every string that the user can see. Change the wording in this file only.</summary>
internal static class Strings
{
    // Window header.
    public const string Enabled = "Enabled";
    public const string TabTiming = "Timing";
    public const string TabContexts = "Contexts";
    public const string TabAlerts = "Alerts";
    public const string TabGeneral = "General";
    public const string TabStatus = "Status";

    // Timing tab.
    public const string BackgroundEnabled = "When the game is in the background";
    public const string BackgroundAfter = "after";
    public const string AwayEnabled = "When I'm away (no input)";
    public const string AwayAfter = "after";
    public const string FadeTime = "Fade to black";
    public const string OnlyWhenLoggedIn = "Only while logged in";
    public const string OnlyWhenLoggedInHint = "Stops it blacking out at the title screen or character select.";
    public const string SliderHint = "Ctrl+Click any slider to type an exact value.";

    // Contexts tab.
    public const string ContextsHint1 = "When several apply, the most relaxed wins: Off beats everything,";
    public const string ContextsHint2 = "otherwise the longest delay is used. Green = active right now.";
    public const string ColumnContext = "Context";
    public const string ColumnBehavior = "Behavior";
    public const string ColumnBackground = "In background";
    public const string ColumnAway = "Away";
    public const string Default = "default";
    public const string Dash = "-";

    // Alerts tab.
    public const string WakeSection = "Wake me when";
    public const string WakeDuty = "A duty is ready";
    public const string WakeDutyHint = "The picture stays on while the popup is up.";
    public const string WakeTell = "I get a direct message";
    public const string WakeStay = "Stay visible for";
    public const string WakeHint = "Sound comes back with the picture.";
    public const string ReminderSection = "Reminder while blacked out";
    public const string ReminderEnabled = "Remind me the game is still running";
    public const string ReminderEvery = "every";
    public const string ReminderBrightness = "brightness";
    public const string ReminderShowFor = "for";
    public const string ReminderHint = "Dim text in a new spot each time, so it can't burn in.";

    // General tab.
    public const string AudioSection = "Audio";
    public const string MuteEnabled = "Mute game audio while blacked out";
    public const string DtrSection = "Server info bar";
    public const string DtrEnabled = "Show a countdown before blacking out";
    public const string DtrLead = "appears within";
    public const string DtrHint = "Hidden the rest of the time, and while blacked out.";

    // Footer.
    public const string Preview = "Preview (5s)";
    public const string Reset = "Reset to defaults";
    public const string ResetConfirm = "Click again to reset";

    // Status tab.
    public const string StatusNow = "Now";
    public const string StatusIdle = "Idle";
    public const string StatusContext = "Context";
    public const string StatusNext = "Blacks out";

    // Status values.
    public const string Disabled = "Disabled";
    public const string WaitingForLogin = "Waiting for login";
    public const string Watching = "Watching";

    // Chat.
    public const string CommandUsage = "Usage: /blackout [now | preview | on | off]";
    public const string EnabledMessage = "Blackout is enabled.";
    public const string DisabledMessage = "Blackout is disabled.";

    // Reminder and server info bar.
    public const string ReminderLine1 = "FINAL FANTASY XIV is still running";
    public const string DtrTooltip = "The screen will go black shortly. Move the mouse to postpone.";

    // Wake notes.
    public const string WakeDutyNote = "a duty is ready";
    public const string WakeTellNote = "a direct message";

    public static string ReminderLine2(long blackForMs) => $"Black for {Format.Duration(blackForMs)} - any input to return";

    public static string DtrText(long remainingMs) => $"Blackout {Format.Duration(remainingMs)}";

    public static string Context(GameContext context) => context switch
    {
        GameContext.Cutscene => "Cutscene",
        GameContext.GPose => "GPose",
        GameContext.Loading => "Loading screens",
        GameContext.InDuty => "In duty",
        GameContext.Crafting => "Crafting",
        GameContext.Gathering => "Gathering / fishing",
        GameContext.InCombat => "In combat",
        GameContext.DutyQueue => "Duty queue",
        GameContext.Mounted => "Mounted / flying",
        GameContext.Performing => "Performing",
        GameContext.Housing => "Housing",
        GameContext.PvP => "PvP",
        _ => context.ToString(),
    };

    public static string Mode(ContextMode mode) => mode switch
    {
        ContextMode.On => "On",
        ContextMode.Custom => "On, custom timing",
        ContextMode.Off => "Off",
        _ => mode.ToString(),
    };

    public static string Channel(AudioChannel channel) => channel switch
    {
        AudioChannel.Bgm => "Background music",
        AudioChannel.Se => "Sound effects",
        AudioChannel.Voice => "Voice",
        AudioChannel.Env => "Ambience",
        AudioChannel.System => "System sounds",
        AudioChannel.Perform => "Performances",
        _ => channel.ToString(),
    };

    public static string StatusSummary(Configuration config, BlackoutStatus status)
    {
        if (!config.Enabled)
            return Disabled;

        if (status.WaitingForLogin)
            return WaitingForLogin;

        if (status.WakeNote is { } note)
            return note;

        return status.Reason switch
        {
            BlackoutReason.Preview => "Preview",
            BlackoutReason.Manual => "Blacked out (command)",
            BlackoutReason.Away => "Blacked out (away)",
            BlackoutReason.Background => "Blacked out (background)",
            _ when status.BlackoutInMs is { } ms => $"Blacks out in {Format.Duration(ms)}",
            _ => Watching,
        };
    }

    public static string StatusNowText(Configuration config, BlackoutStatus status)
    {
        if (!config.Enabled)
            return Disabled;

        if (status.WaitingForLogin)
            return WaitingForLogin;

        if (status.WakeNote is { } note)
            return $"Watching - woken by {note}";

        return status.Reason switch
        {
            BlackoutReason.Preview => "Black (preview)",
            BlackoutReason.Manual => "Black (command)",
            BlackoutReason.Away => "Black (you're away)",
            BlackoutReason.Background => "Black (game in the background)",
            _ when status.Focused => "Watching (game focused)",
            _ => $"In the background for {Format.Duration(status.BackgroundForMs)}",
        };
    }

    public static string StatusIdleText(BlackoutStatus status) => Format.Duration(status.IdleMs);

    public static string StatusContextsText(BlackoutStatus status)
    {
        if (status.ActiveContexts.Count == 0)
            return "None";

        var text = string.Join(", ", status.ActiveContexts.Select(Context));
        if (status.TurnedOffBy is { } off)
            text += $" - off ({Context(off)})";
        else if (status.UsingCustomTiming)
            text += " - custom timing";

        return text;
    }

    public static string StatusNextText(Configuration config, BlackoutStatus status)
    {
        if (!config.Enabled || status.WaitingForLogin)
            return Dash;

        if (status.Reason is BlackoutReason.Away or BlackoutReason.Background or BlackoutReason.Manual)
            return $"Black for {Format.Duration(status.BlackForMs)}";

        if (status.TurnedOffBy is { } off)
            return $"Not while: {Context(off)}";

        if (status.BlackoutInMs is { } ms)
            return $"in {Format.Duration(ms)}";

        return "Never (both timers are off)";
    }
}
