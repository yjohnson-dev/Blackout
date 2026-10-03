using System;

namespace Blackout;

/// <summary>Moves a value between 0 and 1. A return to zero is immediate.</summary>
internal sealed class Fade
{
    public float Value { get; private set; }

    public void Update(long deltaMs, int fadeMs, bool black)
    {
        if (!black)
        {
            this.Value = 0f;
            return;
        }

        this.Value = fadeMs <= 0
            ? 1f
            : Math.Min(1f, this.Value + (deltaMs <= 0 ? 1f : (float)deltaMs / fadeMs));
    }
}
