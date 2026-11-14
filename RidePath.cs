using System;
using Microsoft.Xna.Framework;

namespace HorseFastTravel;

/// <summary>A visible ride measured in elapsed animation time, never game time-of-day.</summary>
internal sealed class RidePath
{
    public Vector2 Start { get; }
    public Vector2 End { get; }
    public int Direction { get; }

    public RidePath(Vector2 start, Vector2 end, int direction)
    {
        this.Start = start;
        this.End = end;
        this.Direction = direction;
    }

    public Vector2 PositionAt(double elapsedMs, int durationMs)
        => Vector2.Lerp(this.Start, this.End, (float)Math.Clamp(elapsedMs / durationMs, 0, 1));

    /// <summary>The departure is a cinematic: ignore scenery, but keep the ride inside the map.</summary>
    public static RidePath CreateCinematic(Vector2 start, Rectangle footprint, int mapWidth, int mapHeight)
    {
        Vector2[] directions = { Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX, -Vector2.UnitY };
        int[] facing = { 1, 2, 3, 0 };
        int[] room = { mapWidth - footprint.Right, mapHeight - footprint.Bottom, footprint.Left, footprint.Top };
        int best = 0;
        int distance = 0;
        for (int d = 0; d < directions.Length; d++)
        {
            int available = Math.Clamp(room[d] - 8, 0, 320);
            if (available > distance)
            {
                best = d;
                distance = available;
            }
        }
        return new RidePath(start, start + directions[best] * distance, facing[best]);
    }

}
