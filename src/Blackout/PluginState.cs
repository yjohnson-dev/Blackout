using System.Collections.Generic;
using System.IO;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace Blackout;

/// <summary>Runtime state that must survive a crash. It is not a user preference.</summary>
public sealed class PluginState
{
    public bool AudioMutedByUs { get; set; }

    public Dictionary<AudioChannel, bool> SavedAudio { get; set; } = new();
}

/// <summary>Stores PluginState next to the plugin configuration, not inside it.</summary>
public sealed class StateStore
{
    private readonly string path;

    public StateStore(IDalamudPluginInterface pluginInterface)
    {
        this.path = Path.Combine(pluginInterface.GetPluginConfigDirectory(), "state.json");
        this.State = this.Load();
    }

    public PluginState State { get; }

    public void Save()
    {
        try
        {
            File.WriteAllText(this.path, JsonConvert.SerializeObject(this.State));
        }
        catch
        {
            // A failed state write must never stop the plugin.
        }
    }

    private PluginState Load()
    {
        try
        {
            if (!File.Exists(this.path))
                return new PluginState();

            return JsonConvert.DeserializeObject<PluginState>(File.ReadAllText(this.path)) ?? new PluginState();
        }
        catch
        {
            return new PluginState();
        }
    }
}
