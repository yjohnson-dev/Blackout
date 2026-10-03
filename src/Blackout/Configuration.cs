using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Newtonsoft.Json;

namespace Blackout;

public enum ContextMode
{
    On,
    Custom,
    Off,
}

public enum AudioChannel
{
    Bgm,
    Se,
    Voice,
    Env,
    System,
    Perform,
}

[Serializable]
public sealed class ContextSettings
{
    public ContextMode Mode { get; set; } = ContextMode.On;

    public int BackgroundSeconds { get; set; } = Configuration.DefaultBackgroundSeconds;

    public int AwayMinutes { get; set; } = Configuration.DefaultAwayMinutes;
}

/// <summary>User settings only. Transient recovery state lives in PluginState.</summary>
[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public const int DefaultBackgroundSeconds = 10;
    public const int DefaultAwayMinutes = 5;

    public int Version { get; set; } = 1;

    public bool Enabled { get; set; } = true;

    public bool OnlyWhenLoggedIn { get; set; }

    public bool BackgroundEnabled { get; set; } = true;

    public int BackgroundSeconds { get; set; } = DefaultBackgroundSeconds;

    public bool AwayEnabled { get; set; } = true;

    public int AwayMinutes { get; set; } = DefaultAwayMinutes;

    public int FadeMs { get; set; } = 400;

    public bool ReminderEnabled { get; set; } = true;

    public int ReminderIntervalSeconds { get; set; } = 60;

    public int ReminderBrightnessPct { get; set; } = 25;

    public int ReminderDurationMs { get; set; } = 4000;

    public bool DtrEnabled { get; set; } = true;

    public int DtrLeadSeconds { get; set; } = 30;

    public bool MuteEnabled { get; set; }

    // Replace stops Json.NET from appending to these collections on every load.
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public HashSet<AudioChannel> MuteChannels { get; set; } = new() { AudioChannel.Bgm, AudioChannel.Se, AudioChannel.Voice };

    public bool WakeDutyReady { get; set; } = true;

    public bool WakeTell { get; set; }

    public int WakeSeconds { get; set; } = 20;

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<GameContext, ContextSettings> Contexts { get; set; } = new();

    public void EnsureDefaults()
    {
        this.Contexts ??= new Dictionary<GameContext, ContextSettings>();
        this.MuteChannels ??= new HashSet<AudioChannel> { AudioChannel.Bgm, AudioChannel.Se, AudioChannel.Voice };

        foreach (var context in GameContexts.All)
        {
            if (!this.Contexts.ContainsKey(context))
                this.Contexts[context] = GameContexts.DefaultFor(context);
        }
    }

    public ContextSettings Settings(GameContext context)
    {
        if (!this.Contexts.TryGetValue(context, out var settings))
            this.Contexts[context] = settings = GameContexts.DefaultFor(context);

        return settings;
    }

    public void ResetToDefaults()
    {
        this.Enabled = true;
        this.OnlyWhenLoggedIn = false;
        this.BackgroundEnabled = true;
        this.BackgroundSeconds = DefaultBackgroundSeconds;
        this.AwayEnabled = true;
        this.AwayMinutes = DefaultAwayMinutes;
        this.FadeMs = 400;
        this.ReminderEnabled = true;
        this.ReminderIntervalSeconds = 60;
        this.ReminderBrightnessPct = 25;
        this.ReminderDurationMs = 4000;
        this.DtrEnabled = true;
        this.DtrLeadSeconds = 30;
        this.MuteEnabled = false;
        this.MuteChannels = new HashSet<AudioChannel> { AudioChannel.Bgm, AudioChannel.Se, AudioChannel.Voice };
        this.WakeDutyReady = true;
        this.WakeTell = false;
        this.WakeSeconds = 20;
        this.Contexts = new Dictionary<GameContext, ContextSettings>();
        this.EnsureDefaults();
    }
}
