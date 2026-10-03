using System;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Plugin.Services;

namespace Blackout;

/// <summary>
/// A server information bar entry that shows a countdown only while a blackout is near. It has no
/// click action, because a click already moves the mouse and delays the blackout.
/// </summary>
public sealed class ServerInfoBar : IDisposable
{
    private const string Title = "Blackout";

    private readonly IDtrBar dtr;
    private readonly IPluginLog log;
    private IDtrBarEntry? entry;
    private string? lastText;
    private bool unavailable;

    public ServerInfoBar(IDtrBar dtr, IPluginLog log)
    {
        this.dtr = dtr;
        this.log = log;
    }

    public void Update(Configuration config, bool blackedOut, long? nextBlackoutMs)
    {
        if (this.entry is null)
        {
            if (this.unavailable)
                return;

            try
            {
                this.entry = this.dtr.Get(Title);
            }
            catch (ArgumentException)
            {
                // The title is taken. Usually a second copy of Blackout is loaded. Do not retry
                // every frame.
                this.unavailable = true;
                this.log.Warning("Blackout cannot create its server info bar entry, because the title is already in use. Another copy of Blackout is probably loaded. The bar stays unused until the plugin is reloaded.");
                return;
            }
        }

        var lead = config.DtrLeadSeconds * 1000L;
        var show = config.Enabled && config.DtrEnabled && !blackedOut
            && nextBlackoutMs is { } next && next <= lead;

        this.entry.Shown = show;

        if (!show)
        {
            this.lastText = null;
            return;
        }

        var text = Strings.DtrText(nextBlackoutMs!.Value);
        if (text == this.lastText)
            return;

        this.entry.Text = text;
        this.entry.Tooltip = Strings.DtrTooltip;
        this.lastText = text;
    }

    public void Dispose() => this.entry?.Remove();
}
