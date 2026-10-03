using System;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;

namespace Blackout;

public enum GameContext
{
    Cutscene,
    GPose,
    Loading,
    InDuty,
    Crafting,
    Gathering,
    InCombat,
    DutyQueue,
    Mounted,
    Performing,
    Housing,
    PvP,
}

public static class GameContexts
{
    public static readonly GameContext[] All = Enum.GetValues<GameContext>();

    public static ContextSettings DefaultFor(GameContext context) => new()
    {
        Mode = context is GameContext.Cutscene or GameContext.GPose or GameContext.Loading
            ? ContextMode.Off
            : ContextMode.On,
    };

    public static bool IsActive(GameContext context, ICondition c, IClientState clientState) => context switch
    {
        GameContext.Cutscene => c[ConditionFlag.OccupiedInCutSceneEvent] || c[ConditionFlag.WatchingCutscene] || c[ConditionFlag.WatchingCutscene78],
        GameContext.GPose => clientState.IsGPosing,
        GameContext.Loading => c[ConditionFlag.BetweenAreas] || c[ConditionFlag.BetweenAreas51] || c[ConditionFlag.LoggingOut],
        GameContext.InDuty => c[ConditionFlag.BoundByDuty] || c[ConditionFlag.BoundByDuty56] || c[ConditionFlag.BoundByDuty95] || c[ConditionFlag.InDeepDungeon],
        GameContext.Crafting => c[ConditionFlag.Crafting] || c[ConditionFlag.PreparingToCraft] || c[ConditionFlag.ExecutingCraftingAction],
        GameContext.Gathering => c[ConditionFlag.Gathering] || c[ConditionFlag.ExecutingGatheringAction] || c[ConditionFlag.Fishing],
        GameContext.InCombat => c[ConditionFlag.InCombat],
        GameContext.DutyQueue => c[ConditionFlag.InDutyQueue] || c[ConditionFlag.WaitingForDuty] || c[ConditionFlag.WaitingForDutyFinder],
        GameContext.Mounted => c[ConditionFlag.Mounted] || c[ConditionFlag.RidingPillion] || c[ConditionFlag.InFlight],
        GameContext.Performing => c[ConditionFlag.Performing],
        GameContext.Housing => c[ConditionFlag.UsingHousingFunctions],
        GameContext.PvP => clientState.IsPvP,
        _ => false,
    };
}
