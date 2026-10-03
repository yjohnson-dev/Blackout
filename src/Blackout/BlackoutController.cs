using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Plugin.Services;

namespace Blackout;

public enum BlackoutReason
{
    None,
    Preview,
    Manual,
    Away,
    Background,
}

/// <summary>The current decision. It is reused every frame, so the update allocates nothing.</summary>
public sealed class BlackoutStatus
{
    public BlackoutReason Reason;
    public bool Focused;
    public long BackgroundForMs;
    public long IdleMs;
    public long? BlackoutInMs;
    public long BlackForMs;
    public List<GameContext> ActiveContexts = new();
    public GameContext? TurnedOffBy;
    public bool UsingCustomTiming;
    public string? WakeNote;
    public bool WaitingForLogin;
}

/// <summary>Decides if the screen must be black. It also controls the fade.</summary>
public sealed class BlackoutController
{
    private const float StickDeadzone = 25f;
    public const long PreviewMs = 5000;

    private static readonly GamepadButtons[] Buttons = Enum.GetValues<GamepadButtons>();

    private readonly Configuration config;
    private readonly ICondition condition;
    private readonly IClientState clientState;
    private readonly IGamepadState gamepad;
    private readonly WakeWatcher wake;

    private readonly BlackoutStatus status = new();
    private readonly Fade fade = new();
    private readonly ManualBlackout manual = new();

    private long lastUpdate = Clock.Now;
    private long lastGamepadInput = Clock.Now;
    private long? unfocusedSince;
    private long previewUntil;
    private long previewGraceUntil;
    private uint previewInputBaseline;
    private long blackSince;

    public BlackoutController(
        Configuration config,
        ICondition condition,
        IClientState clientState,
        IGamepadState gamepad,
        WakeWatcher wake)
    {
        this.config = config;
        this.condition = condition;
        this.clientState = clientState;
        this.gamepad = gamepad;
        this.wake = wake;
    }

    public BlackoutStatus Status => this.status;

    public float Alpha => this.fade.Value;

    public bool BlackedOut => this.status.Reason != BlackoutReason.None;

    public void StartPreview()
    {
        var now = Clock.Now;
        this.previewUntil = now + PreviewMs;
        this.previewGraceUntil = now + 400; // ignore the click or key that started the test
        this.previewInputBaseline = Win32.LastInputTick();
    }

    public void ToggleManual() => this.manual.Toggle();

    public void Update()
    {
        var now = Clock.Now;
        var delta = now - this.lastUpdate;
        this.lastUpdate = now;

        if (this.IsGamepadActive())
            this.lastGamepadInput = now;

        var focused = Win32.IsGameFocused();
        if (focused)
            this.unfocusedSince = null;
        else
            this.unfocusedSince ??= now;

        this.manual.Update(now, Win32.LastInputTick(), this.lastGamepadInput);

        if (now < this.previewUntil)
        {
            if (now < this.previewGraceUntil)
                this.previewInputBaseline = Win32.LastInputTick();
            else if (Win32.LastInputTick() != this.previewInputBaseline)
                this.previewUntil = 0;
        }

        var s = this.status;
        s.ActiveContexts.Clear();
        s.Focused = focused;
        s.BackgroundForMs = this.unfocusedSince is { } since ? now - since : 0;
        var sinceGamepad = now - this.lastGamepadInput;
        s.IdleMs = Math.Min(Clock.IdleMs, sinceGamepad);
        s.TurnedOffBy = null;
        s.UsingCustomTiming = false;
        s.BlackoutInMs = null;
        s.WakeNote = null;
        s.WaitingForLogin = false;

        var autoReason = this.EvaluateAuto(focused, sinceGamepad, s);
        var waking = this.IsWaking(now, out var wakeNote);
        s.WakeNote = wakeNote;

        var previous = s.Reason;
        var reason = now < this.previewUntil ? BlackoutReason.Preview
            : waking ? BlackoutReason.None
            : this.manual.Active && this.config.Enabled ? BlackoutReason.Manual
            : autoReason;

        this.fade.Update(delta, this.config.FadeMs, reason != BlackoutReason.None);

        if (reason == BlackoutReason.None)
        {
            s.BlackForMs = 0;
        }
        else
        {
            if (previous == BlackoutReason.None)
                this.blackSince = now;

            s.BlackForMs = now - this.blackSince;
        }

        s.Reason = reason;
    }

    private BlackoutReason EvaluateAuto(bool focused, long sinceGamepad, BlackoutStatus s)
    {
        if (!this.config.Enabled)
            return BlackoutReason.None;

        if (this.config.OnlyWhenLoggedIn && !this.clientState.IsLoggedIn)
        {
            s.WaitingForLogin = true;
            return BlackoutReason.None;
        }

        long? background = null;
        long? away = null;

        foreach (var context in GameContexts.All)
        {
            if (!GameContexts.IsActive(context, this.condition, this.clientState))
                continue;

            s.ActiveContexts.Add(context);
            var settings = this.config.Settings(context);

            if (settings.Mode == ContextMode.Off)
            {
                s.TurnedOffBy ??= context;
                continue;
            }

            var custom = settings.Mode == ContextMode.Custom;
            s.UsingCustomTiming |= custom;

            var backgroundMs = (custom ? settings.BackgroundSeconds : this.config.BackgroundSeconds) * 1000L;
            var awayMs = (custom ? settings.AwayMinutes : this.config.AwayMinutes) * 60_000L;
            background = Math.Max(background ?? 0, backgroundMs);
            away = Math.Max(away ?? 0, awayMs);
        }

        if (s.TurnedOffBy != null)
            return BlackoutReason.None;

        var backgroundDelay = background ?? this.config.BackgroundSeconds * 1000L;
        var awayDelay = away ?? this.config.AwayMinutes * 60_000L;

        long? remainingAway = this.config.AwayEnabled ? awayDelay - s.IdleMs : null;
        long? remainingBackground = this.config.BackgroundEnabled && !focused
            ? backgroundDelay - Math.Min(s.BackgroundForMs, sinceGamepad)
            : null;

        var reason = BlackoutReason.None;
        if (remainingAway is { } awayLeft && awayLeft <= 0)
            reason = BlackoutReason.Away;
        else if (remainingBackground is { } backgroundLeft && backgroundLeft <= 0)
            reason = BlackoutReason.Background;

        if (remainingAway != null || remainingBackground != null)
            s.BlackoutInMs = Math.Max(0, Math.Min(remainingAway ?? long.MaxValue, remainingBackground ?? long.MaxValue));

        return reason;
    }

    private bool IsWaking(long now, out string? note)
    {
        if (this.config.WakeDutyReady && this.wake.DutyPopupVisible)
        {
            note = Strings.WakeDutyNote;
            return true;
        }

        if (this.config.WakeTell && this.wake.LastTellTick != long.MinValue
            && now - this.wake.LastTellTick <= this.config.WakeSeconds * 1000L)
        {
            note = Strings.WakeTellNote;
            return true;
        }

        note = null;
        return false;
    }

    private bool IsGamepadActive()
    {
        foreach (var button in Buttons)
        {
            if (button != GamepadButtons.None && this.gamepad.Raw(button) != 0)
                return true;
        }

        return this.gamepad.LeftStick.Length() > StickDeadzone || this.gamepad.RightStick.Length() > StickDeadzone;
    }
}
