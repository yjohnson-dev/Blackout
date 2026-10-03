using System;
using Dalamud.Game.Config;
using Dalamud.Plugin.Services;

namespace Blackout;

/// <summary>
/// Mutes the selected audio channels while the screen is black, then restores them. It never
/// changes the master volume. The previous channel states are kept in PluginState, so a crash
/// cannot leave the game muted.
/// </summary>
public sealed class AudioMuter
{
    private readonly IGameConfig gameConfig;
    private readonly Configuration config;
    private readonly StateStore stateStore;
    private readonly IPluginLog log;

    private bool muted;

    public AudioMuter(IGameConfig gameConfig, Configuration config, StateStore stateStore, IPluginLog log)
    {
        this.gameConfig = gameConfig;
        this.config = config;
        this.stateStore = stateStore;
        this.log = log;
        this.RestoreAfterCrash();
    }

    private PluginState State => this.stateStore.State;

    public void SetMuted(bool mute)
    {
        if (mute == this.muted)
            return;

        if (mute)
        {
            // Read all channel states before you change any of them.
            this.State.SavedAudio.Clear();
            foreach (var channel in this.config.MuteChannels)
            {
                if (this.TryGet(channel, out var wasMuted))
                    this.State.SavedAudio[channel] = wasMuted;
            }

            foreach (var channel in this.config.MuteChannels)
                this.TrySet(channel, true);

            this.State.AudioMutedByUs = true;
            this.stateStore.Save();
        }
        else
        {
            foreach (var (channel, wasMuted) in this.State.SavedAudio)
                this.TrySet(channel, wasMuted);

            this.State.SavedAudio.Clear();
            this.State.AudioMutedByUs = false;
            this.stateStore.Save();
        }

        this.muted = mute;
    }

    public void Dispose()
    {
        foreach (var channel in this.config.MuteChannels)
            this.TrySet(channel, false);

        this.State.SavedAudio.Clear();
        this.State.AudioMutedByUs = false;
        this.stateStore.Save();
        this.muted = false;
    }

    private void RestoreAfterCrash()
    {
        if (!this.State.AudioMutedByUs)
            return;

        // The saved values can be wrong after a crash, so enable the channels that Blackout manages.
        foreach (var channel in this.config.MuteChannels)
            this.TrySet(channel, false);

        this.State.SavedAudio.Clear();
        this.State.AudioMutedByUs = false;
        this.stateStore.Save();
        this.log.Info("Blackout enabled the audio channels that a previous session muted.");
    }

    private static SystemConfigOption Option(AudioChannel channel) => channel switch
    {
        AudioChannel.Bgm => SystemConfigOption.IsSndBgm,
        AudioChannel.Se => SystemConfigOption.IsSndSe,
        AudioChannel.Voice => SystemConfigOption.IsSndVoice,
        AudioChannel.Env => SystemConfigOption.IsSndEnv,
        AudioChannel.System => SystemConfigOption.IsSndSystem,
        AudioChannel.Perform => SystemConfigOption.IsSndPerform,
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
    };

    private bool TryGet(AudioChannel channel, out bool value)
    {
        value = false;
        if (!Enum.IsDefined(channel))
            return false;

        if (this.gameConfig.TryGet(Option(channel), out value))
            return true;

        this.log.Warning($"Blackout cannot read the audio channel {channel}.");
        return false;
    }

    private void TrySet(AudioChannel channel, bool value)
    {
        if (!Enum.IsDefined(channel))
            return;

        try
        {
            this.gameConfig.Set(Option(channel), value);
        }
        catch (Exception ex)
        {
            this.log.Error(ex, $"Blackout cannot set the audio channel {channel}.");
        }
    }
}
