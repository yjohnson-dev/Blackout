using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Blackout;

/// <summary>Draws the black fill and the reminder text.</summary>
internal sealed class Overlay
{
    private const float ReminderFadeMs = 600f;
    private const float Margin = 40f;

    public void Draw(float alpha, Configuration config, BlackoutStatus status)
    {
        var viewport = ImGui.GetMainViewport();
        var drawList = ImGui.GetForegroundDrawList();
        drawList.AddRectFilled(viewport.Pos, viewport.Pos + viewport.Size, Rgba(0, alpha));

        if (alpha < 1f || !config.ReminderEnabled)
            return;

        long cycle;
        float fade;

        if (status.Reason == BlackoutReason.Preview)
        {
            // A preview shows the reminder at once, so the test looks like a real blackout.
            var total = BlackoutController.PreviewMs;
            var elapsed = Math.Clamp(status.BlackForMs, 0, total);
            fade = Math.Clamp(Math.Min(elapsed, total - elapsed) / ReminderFadeMs, 0f, 1f);
            cycle = 0;
        }
        else
        {
            var interval = config.ReminderIntervalSeconds * 1000L;
            var showMs = config.ReminderDurationMs;
            if (status.BlackForMs < interval)
                return;

            cycle = (status.BlackForMs - interval) / interval;
            var phase = (status.BlackForMs - interval) % interval;
            if (phase >= showMs)
                return;

            fade = Math.Clamp(Math.Min(phase, showMs - phase) / ReminderFadeMs, 0f, 1f);
        }

        var grey = (int)Math.Round(255.0 * config.ReminderBrightnessPct / 100.0);

        var line1 = Strings.ReminderLine1;
        var line2 = Strings.ReminderLine2(status.BlackForMs);
        var size1 = ImGui.CalcTextSize(line1);
        var size2 = ImGui.CalcTextSize(line2);
        var spacing = size1.Y * 0.4f;
        var block = new Vector2(Math.Max(size1.X, size2.X), size1.Y + spacing + size2.Y);

        // The text moves to a new position on each interval, so no pixel stays lit.
        var random = new Random((int)cycle);
        var room = Vector2.Max(Vector2.Zero, viewport.Size - block - new Vector2(Margin * 2));
        var origin = viewport.Pos + new Vector2(Margin)
            + new Vector2((float)random.NextDouble() * room.X, (float)random.NextDouble() * room.Y);

        var color = Rgba(grey, fade);
        drawList.AddText(origin + new Vector2((block.X - size1.X) / 2, 0), color, line1);
        drawList.AddText(origin + new Vector2((block.X - size2.X) / 2, size1.Y + spacing), color, line2);
    }

    private static uint Rgba(int grey, float alpha)
    {
        var a = (uint)Math.Clamp(alpha * 255f, 0f, 255f);
        var g = (uint)Math.Clamp(grey, 0, 255);
        return (a << 24) | (g << 16) | (g << 8) | g;
    }
}
