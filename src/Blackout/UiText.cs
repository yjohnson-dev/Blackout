namespace Blackout;

/// <summary>All text that the user sees. The wording follows ASD-STE100.</summary>
internal static class UiText
{
    public static string Context(GameContext context) => context switch
    {
        GameContext.Cutscene => "Cutscene",
        GameContext.GPose => "Group pose",
        GameContext.Loading => "Loading screens",
        GameContext.InDuty => "Duty",
        GameContext.Crafting => "Crafting",
        GameContext.Gathering => "Gathering and fishing",
        GameContext.InCombat => "Combat",
        GameContext.DutyQueue => "Duty queue",
        GameContext.Mounted => "Mounted or flying",
        GameContext.Performing => "Performance",
        GameContext.Housing => "Housing",
        GameContext.PvP => "Player versus player",
        _ => context.ToString(),
    };

    public static string Mode(ContextMode mode) => mode switch
    {
        ContextMode.On => "On",
        ContextMode.Custom => "On, own timers",
        ContextMode.Off => "Off",
        _ => mode.ToString(),
    };

    public static string Channel(AudioChannel channel) => channel switch
    {
        AudioChannel.Bgm => "Background music",
        AudioChannel.Se => "Sound effects",
        AudioChannel.Voice => "Voice",
        AudioChannel.Env => "Background sounds",
        AudioChannel.System => "System sounds",
        AudioChannel.Perform => "Performances",
        _ => channel.ToString(),
    };
}
