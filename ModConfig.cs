using System;

namespace HorseFastTravel;

public sealed class ModConfig
{
    public int TravelCost { get; set; } = 1000;

    public int AnimationDurationMs { get; set; } = 2000;

    public bool Normalize()
    {
        int cost = Math.Clamp(this.TravelCost, 0, 1_000_000);
        int duration = Math.Clamp(this.AnimationDurationMs, 500, 10_000);
        bool changed = cost != this.TravelCost || duration != this.AnimationDurationMs;
        this.TravelCost = cost;
        this.AnimationDurationMs = duration;
        return changed;
    }
}
