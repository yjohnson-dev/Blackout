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
    private readonly IDtrBar dtr;
    private IDtrBarEntry? entry;
    private string? lastText;

    public ServerInfoBar(IDtrBar dtr) => this.dtr = dtr;

    public void Update(Configuration config, bool blackedOut, long? nextBlackoutMs)
    {
        this.entry ??= this.dtr.Get("Blackout");
        if (this.entry is null)
            return;

        var lead = config.DtrLeadSeconds * 1000L;
        var show = config.Enabled && config.DtrEnabled && !blackedOut
            && nextBlackoutMs is { } next && next <= lead;

        this.entry.Shown = show;

        if (!show)
        {
            this.lastText = null;
            return;
        }

        var text = $"Blackout {Format.Duration(nextBlackoutMs!.Value)}";
        if (text == this.lastText)
            return;

        this.entry.Text = text;
        this.entry.Tooltip = "The screen goes black soon. Move the mouse to delay this.";
        this.lastText = text;
    }

    public void Dispose() => this.entry?.Remove();
}
