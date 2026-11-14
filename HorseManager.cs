using System;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace HorseFastTravel;

internal static class HorseManager
{
    public static Stable? FindStable(Farmer who)
        => Game1.getFarm().buildings.OfType<Stable>().FirstOrDefault(stable =>
            stable.daysOfConstructionLeft.Value <= 0 && stable.owner.Value == who.UniqueMultiplayerID);

    public static Horse? FindHorse(Farmer who, Stable stable)
    {
        // Mounted horses are removed from location.characters by the game.
        Horse? horse = who.mount ?? Utility.findHorse(stable.HorseId);
        return horse?.HorseId == stable.HorseId && horse.ownerId.Value == who.UniqueMultiplayerID ? horse : null;
    }

    public static bool IsAvailable(Farmer who, Horse horse)
        => (horse.rider == null || horse.rider == who)
            && !horse.mounting.Value && !horse.dismounting.Value
            && (who.mount == null || who.mount == horse)
            && (who.mount == horse || !horse.mutex.IsLocked());

    public static void Mount(Farmer who, Horse horse)
    {
        if (who.mount == horse)
            return;
        // Same relationship/collection changes as Horse.update's native mounting completion.
        // checkAction would instead feed/equip a carrot or hat held by the player.
        if (horse.currentLocation != who.currentLocation)
            Game1.warpCharacter(horse, who.currentLocation, who.Position / 64f);
        horse.Position = who.Position;
        horse.Halt();
        horse.currentLocation.characters.Remove(horse);
        horse.rider = who;
        who.mount = horse;
        horse.SyncPositionToRider();
        who.showRiding();
        who.currentLocation.localSound("dwop");
    }

    public static Rectangle BoundsAt(Horse horse, Vector2 position)
    {
        Rectangle bounds = horse.GetBoundingBox();
        bounds.Offset((int)position.X - (int)horse.Position.X, (int)position.Y - (int)horse.Position.Y);
        return bounds;
    }

    public static bool CanOccupy(GameLocation location, Farmer who, Horse horse, Vector2 position)
    {
        location.updateMap();
        Rectangle bounds = BoundsAt(horse, position);
        if (!location.isTileOnMap(bounds.Left / 64, bounds.Top / 64)
            || !location.isTileOnMap((bounds.Right - 1) / 64, (bounds.Bottom - 1) / 64)
            || bounds.Left < 0 || bounds.Top < 0)
            return false;

        if (location.isCollidingPosition(bounds, Game1.viewport, true, 0, false, who,
                pathfinding: true, skipCollisionEffects: true)
            || location.isCollidingWithWarpOrDoor(bounds, who) != null)
            return false;

        for (int x = bounds.Left / 64; x <= (bounds.Right - 1) / 64; x++)
        for (int y = bounds.Top / 64; y <= (bounds.Bottom - 1) / 64; y++)
        {
            if (location.doesTileHaveProperty(x, y, "TouchAction", "Back") != null)
                return false;
        }
        return true;
    }

    public static RidePath CreateRide(GameLocation location, Stable stable, Horse horse)
    {
        location.updateMap();
        int mapWidth = location.Map.Layers[0].LayerWidth * 64;
        int mapHeight = location.Map.Layers[0].LayerHeight * 64;
        // The stable faces south: start below its footprint, aligned with the horse's stall.
        Vector2 start = new(stable.GetDefaultHorseTile().X * 64,
            (stable.tileY.Value + stable.tilesHigh.Value) * 64);
        Rectangle bounds = BoundsAt(horse, start);
        // Only respect map edges here; scenery and actors never block departure.
        int offsetX = Math.Clamp(0, -bounds.Left, mapWidth - bounds.Right);
        int offsetY = Math.Clamp(0, -bounds.Top, mapHeight - bounds.Bottom);
        start += new Vector2(offsetX, offsetY);
        bounds.Offset(offsetX, offsetY);
        return RidePath.CreateCinematic(start, bounds, mapWidth, mapHeight);
    }

    public static Point? FindArrival(GameLocation location, Point preferred, Farmer who, Horse horse)
    {
        for (int radius = 0; radius <= 6; radius++)
        for (int y = -radius; y <= radius; y++)
        for (int x = -radius; x <= radius; x++)
        {
            if (Math.Abs(x) + Math.Abs(y) != radius)
                continue;
            Point tile = new(preferred.X + x, preferred.Y + y);
            if (CanOccupy(location, who, horse, new Vector2(tile.X * 64, tile.Y * 64)))
                return tile;
        }
        return null;
    }
}
