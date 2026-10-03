using System.Linq;

namespace Blackout;

/// <summary>Builds the status text for the window.</summary>
internal static class StatusText
{
    public static string Summary(Configuration config, BlackoutStatus status)
    {
        if (!config.Enabled)
            return "Off";

        if (status.WaitingForLogin)
            return "Waiting for you to log in";

        if (status.WakeNote is { } note)
            return note;

        return status.Reason switch
        {
            BlackoutReason.Preview => "Black. This is a test.",
            BlackoutReason.Manual => "Black. You used the command.",
            BlackoutReason.Away => "Black. You are away.",
            BlackoutReason.Background => "Black. The game is in the background.",
            _ when status.BlackoutInMs is { } ms => $"Black in {Format.Duration(ms)}",
            _ => "On",
        };
    }

    public static string Now(Configuration config, BlackoutStatus status)
    {
        if (!config.Enabled)
            return "Off";

        if (status.WaitingForLogin)
            return "Waiting for you to log in";

        if (status.WakeNote is { } note)
            return $"On. The screen is not black. {note}.";

        return status.Reason switch
        {
            BlackoutReason.Preview => "Black. This is a test.",
            BlackoutReason.Manual => "Black. You used the command.",
            BlackoutReason.Away => "Black. You are away.",
            BlackoutReason.Background => "Black. The game is in the background.",
            _ when status.Focused => "On. The game has focus.",
            _ => $"On. The game is in the background for {Format.Duration(status.BackgroundForMs)}.",
        };
    }

    public static string Idle(BlackoutStatus status) => Format.Duration(status.IdleMs);

    public static string Contexts(BlackoutStatus status)
    {
        if (status.ActiveContexts.Count == 0)
            return "None";

        var text = string.Join(", ", status.ActiveContexts.Select(UiText.Context));
        if (status.TurnedOffBy is { } off)
            text += $" - off ({UiText.Context(off)})";
        else if (status.UsingCustomTiming)
            text += " - own timers";

        return text;
    }

    public static string Next(Configuration config, BlackoutStatus status)
    {
        if (!config.Enabled || status.WaitingForLogin)
            return "-";

        if (status.Reason is BlackoutReason.Away or BlackoutReason.Background or BlackoutReason.Manual)
            return $"Black for {Format.Duration(status.BlackForMs)}";

        if (status.TurnedOffBy is { } off)
            return $"Not during: {UiText.Context(off)}";

        if (status.BlackoutInMs is { } ms)
            return $"in {Format.Duration(ms)}";

        return "Never. Both timers are off.";
    }
}
