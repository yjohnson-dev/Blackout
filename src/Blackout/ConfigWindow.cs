using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace Blackout;

public sealed class ConfigWindow : Window
{
    private static readonly Vector4 ActiveColor = new(0.55f, 0.85f, 0.55f, 1f);

    private readonly Plugin plugin;
    private long resetConfirmUntil;

    public ConfigWindow(Plugin plugin)
        : base("Blackout###BlackoutConfig")
    {
        this.plugin = plugin;
        this.Size = new Vector2(560, 460);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 320),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    private Configuration Config => this.plugin.Config;

    private float SliderWidth => ImGui.GetFontSize() * 14;

    public override void Draw()
    {
        var config = this.Config;

        if (Checkbox("Enabled", config.Enabled, v => config.Enabled = v))
            this.plugin.MarkDirty();

        ImGui.SameLine();
        ImGui.TextDisabled(StatusText.Summary(config, this.plugin.Controller.Status));

        ImGui.Separator();

        if (ImGui.BeginTabBar("BlackoutTabs"))
        {
            using (var tab = ImRaii.TabItem("Timing"))
            {
                if (tab.Success)
                    this.DrawTiming();
            }

            using (var tab = ImRaii.TabItem("Contexts"))
            {
                if (tab.Success)
                    this.DrawContexts();
            }

            using (var tab = ImRaii.TabItem("Alerts"))
            {
                if (tab.Success)
                    this.DrawAlerts();
            }

            using (var tab = ImRaii.TabItem("General"))
            {
                if (tab.Success)
                    this.DrawGeneral();
            }

            using (var tab = ImRaii.TabItem("Status"))
            {
                if (tab.Success)
                    this.DrawStatus();
            }

            ImGui.EndTabBar();
        }

        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Test for 5 seconds"))
            this.plugin.Controller.StartPreview();

        ImGui.SameLine();

        var now = Environment.TickCount64;
        var confirming = now < this.resetConfirmUntil;
        if (ImGui.Button(confirming ? "Click again to reset###reset" : "Reset all settings###reset"))
        {
            if (confirming)
            {
                this.Config.ResetToDefaults();
                this.plugin.MarkDirty();
                this.resetConfirmUntil = 0;
            }
            else
            {
                this.resetConfirmUntil = now + 3000;
            }
        }
    }

    private void DrawTiming()
    {
        var config = this.Config;

        if (Checkbox("When the game is in the background", config.BackgroundEnabled, v => config.BackgroundEnabled = v))
            this.plugin.MarkDirty();
        this.Slider("after", config.BackgroundSeconds, 3, 120, "%d sec", v => config.BackgroundSeconds = v, !config.BackgroundEnabled);

        ImGui.Spacing();

        if (Checkbox("When you are away (no input)", config.AwayEnabled, v => config.AwayEnabled = v))
            this.plugin.MarkDirty();
        this.Slider("after", config.AwayMinutes, 1, 30, "%d min", v => config.AwayMinutes = v, !config.AwayEnabled);

        ImGui.Spacing();
        this.Slider("Fade time", config.FadeMs, 0, 1000, "%d ms", v => config.FadeMs = v, false);

        ImGui.Spacing();

        if (Checkbox("Only while logged in", config.OnlyWhenLoggedIn, v => config.OnlyWhenLoggedIn = v))
            this.plugin.MarkDirty();

        ImGui.TextDisabled("This stops the blackout at the title screen and the character select screen.");
        ImGui.TextDisabled("Press Ctrl and click a slider to type a value.");
    }

    private void DrawContexts()
    {
        var config = this.Config;
        var active = this.plugin.Controller.Status.ActiveContexts;

        ImGui.TextDisabled("If more than one context applies, the most relaxed setting wins.");
        ImGui.TextDisabled("Off wins over all. A green name means the context is active now.");
        ImGui.Spacing();

        const ImGuiTableFlags flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp;
        using var table = ImRaii.Table("contexts", 4, flags);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Context", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFontSize() * 11);
        ImGui.TableSetupColumn("Behavior", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFontSize() * 12);
        ImGui.TableSetupColumn("In background", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Away", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        foreach (var context in GameContexts.All)
        {
            var settings = config.Settings(context);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var label = UiText.Context(context);
            if (active.Contains(context))
                ImGui.TextColored(ActiveColor, label);
            else
                ImGui.Text(label);

            using var id = ImRaii.PushId(context.ToString());

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            using (var combo = ImRaii.Combo("##mode", UiText.Mode(settings.Mode)))
            {
                if (combo.Success)
                {
                    foreach (var mode in Enum.GetValues<ContextMode>())
                    {
                        if (ImGui.Selectable(UiText.Mode(mode), mode == settings.Mode))
                        {
                            settings.Mode = mode;
                            this.plugin.MarkDirty();
                        }
                    }
                }
            }

            var custom = settings.Mode == ContextMode.Custom;

            ImGui.TableNextColumn();
            if (custom)
            {
                ImGui.SetNextItemWidth(-1);
                if (Slider("##background", settings.BackgroundSeconds, 3, 600, "%d sec", v => settings.BackgroundSeconds = v))
                    this.plugin.MarkDirty();
            }
            else
            {
                ImGui.TextDisabled(settings.Mode == ContextMode.On ? "default" : "-");
            }

            ImGui.TableNextColumn();
            if (custom)
            {
                ImGui.SetNextItemWidth(-1);
                if (Slider("##away", settings.AwayMinutes, 1, 120, "%d min", v => settings.AwayMinutes = v))
                    this.plugin.MarkDirty();
            }
            else
            {
                ImGui.TextDisabled(settings.Mode == ContextMode.On ? "default" : "-");
            }
        }
    }

    private void DrawAlerts()
    {
        var config = this.Config;

        Section("Wake the screen");
        if (Checkbox("A duty is ready", config.WakeDutyReady, v => config.WakeDutyReady = v))
            this.plugin.MarkDirty();
        ImGui.TextDisabled("The screen stays on while the duty window is open.");

        if (Checkbox("You get a direct message", config.WakeTell, v => config.WakeTell = v))
            this.plugin.MarkDirty();
        this.Slider("Keep the screen on for", config.WakeSeconds, 5, 60, "%d sec", v => config.WakeSeconds = v, !config.WakeTell);
        ImGui.TextDisabled("The sound returns with the screen.");

        Section("Reminder");
        if (Checkbox("Show a reminder that the game is running", config.ReminderEnabled, v => config.ReminderEnabled = v))
            this.plugin.MarkDirty();
        this.Slider("Every", config.ReminderIntervalSeconds, 15, 600, "%d sec", v => config.ReminderIntervalSeconds = v, !config.ReminderEnabled);
        this.Slider("Brightness", config.ReminderBrightnessPct, 5, 100, "%d %%", v => config.ReminderBrightnessPct = v, !config.ReminderEnabled);
        this.Slider("Show for", config.ReminderDurationMs, 1000, 15000, "%d ms", v => config.ReminderDurationMs = v, !config.ReminderEnabled);
        ImGui.TextDisabled("The text is dim. It moves to a new position each time.");
    }

    private void DrawGeneral()
    {
        var config = this.Config;

        Section("Audio");
        if (Checkbox("Mute the game when the screen is black", config.MuteEnabled, v => config.MuteEnabled = v))
            this.plugin.MarkDirty();

        using (ImRaii.Disabled(!config.MuteEnabled))
        using (ImRaii.PushIndent())
        {
            foreach (var channel in Enum.GetValues<AudioChannel>())
            {
                var on = config.MuteChannels.Contains(channel);
                if (!ImGui.Checkbox(UiText.Channel(channel), ref on))
                    continue;

                if (on)
                    config.MuteChannels.Add(channel);
                else
                    config.MuteChannels.Remove(channel);

                this.plugin.MarkDirty();
            }
        }

        Section("Server information bar");
        if (Checkbox("Show a countdown before the screen goes black", config.DtrEnabled, v => config.DtrEnabled = v))
            this.plugin.MarkDirty();
        this.Slider("Show it this long before", config.DtrLeadSeconds, 5, 120, "%d sec", v => config.DtrLeadSeconds = v, !config.DtrEnabled);
        ImGui.TextDisabled("The countdown is hidden at all other times.");
    }

    private void DrawStatus()
    {
        var config = this.Config;
        var status = this.plugin.Controller.Status;
        var column = ImGui.GetFontSize() * 8;

        Row("Now", column, StatusText.Now(config, status));
        Row("No input for", column, StatusText.Idle(status));
        Row("Situation", column, StatusText.Contexts(status));
        Row("Next blackout", column, StatusText.Next(config, status));
    }

    private void Slider(string label, int value, int min, int max, string format, Action<int> set, bool disabled)
    {
        using (ImRaii.PushIndent())
        using (ImRaii.Disabled(disabled))
        {
            ImGui.SetNextItemWidth(this.SliderWidth);
            if (Slider(label, value, min, max, format, set))
                this.plugin.MarkDirty();
        }
    }

    private static bool Checkbox(string label, bool value, Action<bool> set)
    {
        if (!ImGui.Checkbox(label, ref value))
            return false;
        set(value);
        return true;
    }

    private static bool Slider(string label, int value, int min, int max, string format, Action<int> set)
    {
        if (!ImGui.SliderInt(label, ref value, min, max, format, ImGuiSliderFlags.AlwaysClamp))
            return false;
        set(value);
        return true;
    }

    private static void Section(string text)
    {
        ImGui.Spacing();
        ImGui.Text(text);
        ImGui.Separator();
        ImGui.Spacing();
    }

    private static void Row(string label, float column, string value)
    {
        ImGui.TextDisabled(label);
        ImGui.SameLine(column);
        ImGui.Text(value);
    }
}
