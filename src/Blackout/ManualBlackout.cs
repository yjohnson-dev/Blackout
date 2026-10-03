using System;

namespace Blackout;

/// <summary>
/// The blackout that "/blackout now" starts. It ignores the keys that start it, then stops on the
/// next input.
/// </summary>
internal sealed class ManualBlackout
{
    private const long SettleMs = 250;

    private bool awaitingRelease;
    private long releasedAt;
    private uint inputBaseline;
    private long gamepadBaseline;

    public bool Active { get; private set; }

    public void Toggle()
    {
        if (this.Active)
        {
            this.Active = false;
            return;
        }

        this.Active = true;
        this.awaitingRelease = true;
        this.releasedAt = 0;
    }

    public void Update(long now, uint lastInputTick, long lastGamepadInput)
    {
        if (!this.Active)
            return;

        if (this.awaitingRelease)
        {
            var held = Win32.IsKeyDown(Win32.VkReturn)
                || Win32.IsKeyDown(Win32.VkControl)
                || Win32.IsKeyDown(Win32.VkShift)
                || Win32.IsKeyDown(Win32.VkMenu);

            if (held)
                this.releasedAt = 0;
            else if (this.releasedAt == 0)
                this.releasedAt = now;
            else if (now - this.releasedAt >= SettleMs)
                this.Arm(lastInputTick, lastGamepadInput);

            return;
        }

        if (lastInputTick != this.inputBaseline || lastGamepadInput > this.gamepadBaseline)
            this.Active = false;
    }

    private void Arm(uint lastInputTick, long lastGamepadInput)
    {
        this.awaitingRelease = false;
        this.inputBaseline = lastInputTick;
        this.gamepadBaseline = lastGamepadInput;
    }
}
