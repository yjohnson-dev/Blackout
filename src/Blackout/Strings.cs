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
    public const string BackgroundEnabled = "Turn the screen black when the game is in the background";
    public const string BackgroundAfter = "after";
    public const string AwayEnabled = "Turn the screen black when I'm away (no input)";
    public const string AwayAfter = "after";
    public const string FadeTime = "Fade duration";
    public const string OnlyWhenLoggedIn = "Only while logged in";
    public const string SliderTip = "Tip: Ctrl+click any slider to type an exact value.";

    // Timing help.
    public const string AwayHelp = "Away means no keyboard, mouse, or controller input.";
    public const string FadeTimeHelp = "How long the screen takes to fade to black.";
    public const string OnlyWhenLoggedInHelp = "Keep the screen on at the title screen and character select.";

    // Contexts tab.
    public const string ContextsHint = "Choose how Blackout behaves in each context.";
    public const string ColumnContext = "Context";
    public const string ColumnBehavior = "Behavior";
    public const string ColumnBackground = "In background";
    public const string ColumnAway = "Away";
    public const string Dash = "-";

    // Alerts tab.
    public const string WakeSection = "Bring the screen back when";
    public const string WakeDuty = "A duty pop appears";
    public const string WakeTell = "A tell arrives";
    public const string WakeStay = "Stay on for";
    public const string WakeStayHelp = "How long the screen stays on after a tell.";

    public const string ReminderSection = "Reminder while the screen is black";
    public const string ReminderEnabled = "Show a reminder that the game is still running";
    public const string ReminderEvery = "Repeat every";
    public const string ReminderBrightness = "Text brightness";
    public const string ReminderShowFor = "Show for";

    // General tab.
    public const string AudioSection = "Audio";
    public const string MuteEnabled = "Mute game audio while the screen is black";
    public const string DtrSection = "Server info bar";
    public const string DtrEnabled = "Show a countdown before the screen goes black";
    public const string DtrLead = "Appears within";

    public const string CoverageSection = "Screen coverage";
    public const string KeepWindowsVisible = "Keep plugin windows visible while the screen is black";
    public const string KeepWindowsVisibleHelp = "Renders over the native game rendering and UI, but not other plugin windows.";

    // Footer.
    public const string Preview = "Preview (5s)";
    public const string Reset = "Reset to defaults";
    public const string ResetConfirm = "Click again to reset";

    // Status tab.
    public const string StatusNow = "Now";
    public const string StatusIdle = "Idle";
    public const string StatusContext = "Context";
    public const string StatusNext = "Blackout";
    public const string StatusIdleHelp = "Time since the last keyboard, mouse, or controller input.";

    // Status values.
    public const string Disabled = "Disabled";
    public const string WaitingForLogin = "Waiting for login";
    public const string OnStandby = "On standby";
    public const string NoTriggerEnabled = "No trigger enabled";

    // Chat.
    public const string CommandUsage =
        "Blackout commands:\n" +
        "  /blackout: open settings\n" +
        "  /blackout now: turn the screen black now\n" +
        "  /blackout preview: 5-second preview\n" +
        "  /blackout on / off: turn the plugin on or off";

    public const string EnabledMessage = "Blackout is on.";
    public const string DisabledMessage = "Blackout is off.";

    // Reminder and server info bar.
    public const string ReminderLine1 = "FINAL FANTASY XIV is still running";
    public const string DtrTooltip = "The screen will go black shortly. Move the mouse to postpone.";

    // Wake notes.
    public const string WakeDutyNote = "a duty pop";
    public const string WakeTellNote = "a tell";

    public static string ReminderLine2(long blackForMs) => $"Screen black for {Format.Duration(blackForMs)}. Any input brings it back.";

    public static string DtrText(long remainingMs) => $"Blackout in {Format.Duration(remainingMs)}";

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
        ContextMode.On => "Default timing",
        ContextMode.Custom => "Custom timing",
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
            return $"Paused by {note}";

        if (status.TurnedOffBy is { } off)
            return $"Held off ({Context(off)})";

        if (status.BlackoutInMs is { } ms)
            return $"Screen black in {Format.Duration(ms)}";

        return config.BackgroundEnabled || config.AwayEnabled ? OnStandby : NoTriggerEnabled;
    }

    public static string StatusNowText(Configuration config, BlackoutStatus status)
    {
        if (!config.Enabled)
            return Disabled;

        if (status.WaitingForLogin)
            return WaitingForLogin;

        if (status.WakeNote is { } note)
            return $"Paused by {note}";

        if (status.TurnedOffBy is { } off)
            return $"Held off while {Context(off)}";

        if (!config.BackgroundEnabled && !config.AwayEnabled)
            return NoTriggerEnabled;

        if (status.Focused)
            return "On standby (game focused)";

        return $"Game in the background for {Format.Duration(status.BackgroundForMs)}";
    }

    public static string StatusIdleText(BlackoutStatus status) => Format.Duration(status.IdleMs);

    public static string StatusContextsText(BlackoutStatus status)
    {
        if (status.ActiveContexts.Count == 0)
            return "None";

        var text = string.Join(", ", status.ActiveContexts.Select(Context));
        if (status.TurnedOffBy is { } off)
            text += $" (off while {Context(off)})";
        else if (status.UsingCustomTiming)
            text += " (custom timing)";

        return text;
    }

    public static string StatusNextText(Configuration config, BlackoutStatus status)
    {
        if (!config.Enabled || status.WaitingForLogin)
            return Dash;

        if (status.TurnedOffBy is { } off)
            return $"off while {Context(off)}";

        if (status.BlackoutInMs is { } ms)
            return $"in {Format.Duration(ms)}";

        return "never (both timers are off)";
    }
}
