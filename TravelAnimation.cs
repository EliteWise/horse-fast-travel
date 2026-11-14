using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Pathfinding;

namespace HorseFastTravel;

/// <summary>Uses the native controller slot; native code draws and animates the horse and rider.</summary>
internal sealed class TravelAnimation : PathFindController
{
    private readonly Farmer who;
    private readonly Horse horse;
    private readonly GameLocation source;
    private readonly RidePath ride;
    private readonly int durationMs;
    private double elapsedMs;

    public bool Finished => this.elapsedMs >= this.durationMs;
    public Vector2 StartPosition { get; }
    public Exception? Failure { get; private set; }

    public TravelAnimation(Farmer who, Horse horse, RidePath ride, int durationMs)
        : base(new Stack<Point>(), who.currentLocation, who, Point.Zero)
    {
        this.who = who;
        this.horse = horse;
        this.source = who.currentLocation;
        this.ride = ride;
        this.durationMs = durationMs;
        // Cancellation returns to the player's original position, before staging at the stable.
        this.StartPosition = who.Position;
    }

    public override bool update(GameTime time)
    {
        try
        {
            if (this.Finished || this.Failure != null || Game1.eventUp || Game1.activeClickableMenu != null
                || Game1.isWarping || this.who.currentLocation != this.source || this.who.mount != this.horse)
                return false;

            // A stalled/background frame must not skip the visible ride.
            double nextElapsed = Math.Min(this.durationMs, this.elapsedMs + Math.Clamp(time.ElapsedGameTime.TotalMilliseconds, 0, 100));
            Vector2 next = this.ride.PositionAt(nextElapsed, this.durationMs);
            // This short scripted movement intentionally ignores departure obstacles.
            // Arrival still uses full collision checks before returning player control.
            this.who.movementDirections.Clear();
            this.who.movementDirections.Add(this.ride.Direction);
            this.who.faceDirection(this.ride.Direction);
            this.who.Position = next;
            this.horse.SyncPositionToRider();
            this.elapsedMs = nextElapsed;
            // Farmer.updateCommon then calls Horse.update: native frames, sounds and bobbing.
        }
        catch (Exception ex)
        {
            this.Failure = ex;
        }
        return false;
    }

    public void Stop(bool restoreStart = false)
    {
        if (this.who.controller == this)
        {
            if (restoreStart && this.who.currentLocation == this.source && !Game1.eventUp
                && Game1.CurrentEvent == null && !Game1.isWarping)
            {
                this.who.Position = this.StartPosition;
                this.horse.SyncPositionToRider();
            }
            this.who.controller = null;
            this.who.Halt();
            this.horse.Halt();
            this.horse.Sprite.StopAnimation();
            this.who.showRiding();
        }
    }
}
