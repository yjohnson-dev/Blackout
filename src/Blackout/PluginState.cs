using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
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
    // Do not resolve .NET types from the file. The state file is data, not a type manifest.
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
    };

    private readonly string path;
    private readonly IPluginLog log;

    public StateStore(IDalamudPluginInterface pluginInterface, IPluginLog log)
    {
        this.path = Path.Combine(pluginInterface.GetPluginConfigDirectory(), "state.json");
        this.log = log;
        this.State = this.Load();
    }

    public PluginState State { get; }

    public void Save()
    {
        try
        {
            // Write to a temporary file first, so a crash cannot leave a half-written file.
            var temporary = this.path + ".tmp";
            File.WriteAllText(temporary, JsonConvert.SerializeObject(this.State, SerializerSettings));
            File.Move(temporary, this.path, true);
        }
        catch (Exception ex)
        {
            this.log.Warning(ex, "Blackout cannot save its state file.");
        }
    }

    private PluginState Load()
    {
        try
        {
            if (!File.Exists(this.path))
                return new PluginState();

            return JsonConvert.DeserializeObject<PluginState>(File.ReadAllText(this.path), SerializerSettings)
                ?? new PluginState();
        }
        catch (Exception ex)
        {
            this.log.Warning(ex, "Blackout cannot read its state file. It starts with an empty state.");
            return new PluginState();
        }
    }
}
