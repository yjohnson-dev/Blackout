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

        if (Checkbox(Strings.Enabled, config.Enabled, v => config.Enabled = v))
            this.plugin.MarkDirty();

        ImGui.SameLine();
        ImGui.TextDisabled(Strings.StatusSummary(config, this.plugin.Controller.Status));

        ImGui.Separator();

        if (ImGui.BeginTabBar("BlackoutTabs"))
        {
            using (var tab = ImRaii.TabItem(Strings.TabTiming))
            {
                if (tab.Success)
                    this.DrawTiming();
            }

            using (var tab = ImRaii.TabItem(Strings.TabContexts))
            {
                if (tab.Success)
                    this.DrawContexts();
            }

            using (var tab = ImRaii.TabItem(Strings.TabAlerts))
            {
                if (tab.Success)
                    this.DrawAlerts();
            }

            using (var tab = ImRaii.TabItem(Strings.TabGeneral))
            {
                if (tab.Success)
                    this.DrawGeneral();
            }

            using (var tab = ImRaii.TabItem(Strings.TabStatus))
            {
                if (tab.Success)
                    this.DrawStatus();
            }

            ImGui.EndTabBar();
        }

        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button(Strings.Preview))
            this.plugin.Controller.StartPreview();

        ImGui.SameLine();

        var now = Environment.TickCount64;
        var confirming = now < this.resetConfirmUntil;
        var resetLabel = confirming ? Strings.ResetConfirm : Strings.Reset;
        if (ImGui.Button($"{resetLabel}###reset"))
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

        if (Checkbox(Strings.BackgroundEnabled, config.BackgroundEnabled, v => config.BackgroundEnabled = v))
            this.plugin.MarkDirty();
        this.Slider(Strings.BackgroundAfter, config.BackgroundSeconds, 3, 120, "%d sec", v => config.BackgroundSeconds = v, !config.BackgroundEnabled);

        ImGui.Spacing();

        if (Checkbox(Strings.AwayEnabled, config.AwayEnabled, v => config.AwayEnabled = v))
            this.plugin.MarkDirty();
        this.Slider(Strings.AwayAfter, config.AwayMinutes, 1, 30, "%d min", v => config.AwayMinutes = v, !config.AwayEnabled);

        ImGui.Spacing();
        this.Slider(Strings.FadeTime, config.FadeMs, 0, 1000, "%d ms", v => config.FadeMs = v, false);

        ImGui.Spacing();

        if (Checkbox(Strings.OnlyWhenLoggedIn, config.OnlyWhenLoggedIn, v => config.OnlyWhenLoggedIn = v))
            this.plugin.MarkDirty();

        ImGui.TextDisabled(Strings.OnlyWhenLoggedInHint);
        ImGui.TextDisabled(Strings.SliderHint);
    }

    private void DrawContexts()
    {
        var config = this.Config;
        var active = this.plugin.Controller.Status.ActiveContexts;

        ImGui.TextDisabled(Strings.ContextsHint1);
        ImGui.TextDisabled(Strings.ContextsHint2);
        ImGui.Spacing();

        const ImGuiTableFlags flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp;
        using var table = ImRaii.Table("contexts", 4, flags);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn(Strings.ColumnContext, ImGuiTableColumnFlags.WidthFixed, ImGui.GetFontSize() * 11);
        ImGui.TableSetupColumn(Strings.ColumnBehavior, ImGuiTableColumnFlags.WidthFixed, ImGui.GetFontSize() * 12);
        ImGui.TableSetupColumn(Strings.ColumnBackground, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn(Strings.ColumnAway, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        foreach (var context in GameContexts.All)
        {
            var settings = config.Settings(context);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var label = Strings.Context(context);
            if (active.Contains(context))
                ImGui.TextColored(ActiveColor, label);
            else
                ImGui.Text(label);

            using var id = ImRaii.PushId(context.ToString());

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            using (var combo = ImRaii.Combo("##mode", Strings.Mode(settings.Mode)))
            {
                if (combo.Success)
                {
                    foreach (var mode in Enum.GetValues<ContextMode>())
                    {
                        if (ImGui.Selectable(Strings.Mode(mode), mode == settings.Mode))
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
                ImGui.TextDisabled(settings.Mode == ContextMode.On ? Strings.Default : Strings.Dash);
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
                ImGui.TextDisabled(settings.Mode == ContextMode.On ? Strings.Default : Strings.Dash);
            }
        }
    }

    private void DrawAlerts()
    {
        var config = this.Config;

        Section(Strings.WakeSection);
        if (Checkbox(Strings.WakeDuty, config.WakeDutyReady, v => config.WakeDutyReady = v))
            this.plugin.MarkDirty();
        ImGui.TextDisabled(Strings.WakeDutyHint);

        if (Checkbox(Strings.WakeTell, config.WakeTell, v => config.WakeTell = v))
            this.plugin.MarkDirty();
        this.Slider(Strings.WakeStay, config.WakeSeconds, 5, 60, "%d sec", v => config.WakeSeconds = v, !config.WakeTell);
        ImGui.TextDisabled(Strings.WakeHint);

        Section(Strings.ReminderSection);
        if (Checkbox(Strings.ReminderEnabled, config.ReminderEnabled, v => config.ReminderEnabled = v))
            this.plugin.MarkDirty();
        this.Slider(Strings.ReminderEvery, config.ReminderIntervalSeconds, 15, 600, "%d sec", v => config.ReminderIntervalSeconds = v, !config.ReminderEnabled);
        this.Slider(Strings.ReminderBrightness, config.ReminderBrightnessPct, 5, 100, "%d %%", v => config.ReminderBrightnessPct = v, !config.ReminderEnabled);
        this.Slider(Strings.ReminderShowFor, config.ReminderDurationMs, 1000, 15000, "%d ms", v => config.ReminderDurationMs = v, !config.ReminderEnabled);
        ImGui.TextDisabled(Strings.ReminderHint);
    }

    private void DrawGeneral()
    {
        var config = this.Config;

        Section(Strings.AudioSection);
        if (Checkbox(Strings.MuteEnabled, config.MuteEnabled, v => config.MuteEnabled = v))
            this.plugin.MarkDirty();

        using (ImRaii.Disabled(!config.MuteEnabled))
        using (ImRaii.PushIndent())
        {
            foreach (var channel in Enum.GetValues<AudioChannel>())
            {
                var on = config.MuteChannels.Contains(channel);
                if (!ImGui.Checkbox(Strings.Channel(channel), ref on))
                    continue;

                if (on)
                    config.MuteChannels.Add(channel);
                else
                    config.MuteChannels.Remove(channel);

                this.plugin.MarkDirty();
            }
        }

        Section(Strings.DtrSection);
        if (Checkbox(Strings.DtrEnabled, config.DtrEnabled, v => config.DtrEnabled = v))
            this.plugin.MarkDirty();
        this.Slider(Strings.DtrLead, config.DtrLeadSeconds, 5, 120, "%d sec", v => config.DtrLeadSeconds = v, !config.DtrEnabled);
        ImGui.TextDisabled(Strings.DtrHint);
    }

    private void DrawStatus()
    {
        var config = this.Config;
        var status = this.plugin.Controller.Status;
        var column = ImGui.GetFontSize() * 8;

        Row(Strings.StatusNow, column, Strings.StatusNowText(config, status));
        Row(Strings.StatusIdle, column, Strings.StatusIdleText(status));
        Row(Strings.StatusContext, column, Strings.StatusContextsText(status));
        Row(Strings.StatusNext, column, Strings.StatusNextText(config, status));
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
