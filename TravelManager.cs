using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace HorseFastTravel;

internal sealed class TravelManager
{
    private static readonly SButton[] Buttons = Enum.GetValues<SButton>();
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private Journey? journey;
    private Action? afterMenu;

    public bool IsBusy => this.journey != null;

    private sealed class Journey
    {
        public Farmer Who { get; }
        public Stable Stable { get; }
        public Horse Horse { get; set; }
        public GameLocation Source { get; }
        public GameLocation Destination { get; }
        public Point PreferredArrival { get; }
        public int Cost { get; }
        public TravelPayment Payment { get; }
        public Stopwatch Watch { get; } = Stopwatch.StartNew();
        public TravelAnimation? Animation { get; set; }
        public bool RequestedWarp { get; set; }
        public bool LockedControls { get; set; }
        public bool OwnsNewLock { get; set; }
        public bool FrozeControls { get; set; }
        public bool WaitingForHorse { get; set; }

        public Journey(Farmer who, Stable stable, Horse horse, GameLocation destination, Point arrival, int cost)
        {
            this.Who = who;
            this.Stable = stable;
            this.Horse = horse;
            this.Source = who.currentLocation;
            this.Destination = destination;
            this.PreferredArrival = arrival;
            this.Cost = cost;
            this.Payment = new TravelPayment(amount => who.Money += amount);
        }
    }

    public TravelManager(IModHelper helper, IMonitor monitor, ModConfig config)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.config = config;
    }

    private string T(string key, object? tokens = null) => this.helper.Translation.Get(key, tokens).ToString();

    private static bool CanStart(Farmer who)
        => Context.CanPlayerMove && who == Game1.player && who.controller == null && !who.isAnimatingMount
            && !who.UsingTool && !who.IsSitting() && !who.swimming.Value
            && !Game1.eventUp && Game1.CurrentEvent == null && Game1.farmEvent == null
            && Game1.currentMinigame == null && !Game1.freezeControls && !Game1.isWarping
            && !Game1.globalFade && Game1.fadeToBlackAlpha <= 0 && !Game1.game1.IsSaving
            && !Game1.HostPaused;

    public bool TryOpenMenu(Vector2 target)
    {
        Farmer who = Game1.player;
        if (this.IsBusy || !CanStart(who))
            return false;
        Stable? stable = HorseManager.FindStable(who);
        if (stable == null)
            return false;
        Horse? horse = HorseManager.FindHorse(who, stable);
        if (horse == null)
            return false;

        if (who.currentLocation != Game1.getFarm() || !stable.occupiesTile(target)
            || Vector2.Distance(who.Tile, target) > 2.5f)
            return false;

        // Leave direct interactions with the horse to the game (mount, hats, carrots).
        if (who.mount == null && horse.currentLocation == who.currentLocation
            && horse.GetBoundingBox().Contains((int)(target.X * 64 + 32), (int)(target.Y * 64 + 32)))
            return false;

        GameLocation source = who.currentLocation;
        int quotedCost = this.config.TravelCost;
        string question = this.T("menu.title");
        Response[] responses = { new("travel", this.T("menu.pierre", new { cost = quotedCost })) };
        source.createQuestionDialogue(question, responses, (_, answer) =>
        {
            if (answer != "travel")
                return;
            this.afterMenu = () =>
            {
                if (!CanStart(who) || who.currentLocation != source)
                    return;
                this.StartSelectedTrip(who, stable, horse, quotedCost);
            };
        });
        Game1.activeClickableMenu = new TravelMenu(question, responses, source);
        return true;
    }

    private void StartSelectedTrip(Farmer who, Stable stable, Horse horse, int quotedCost)
    {
        GameLocation destination = Game1.getLocationFromName("Town");
        destination.updateMap();
        Point anchor = destination.getWarpPointTo("SeedShop", who);
        if (anchor == Point.Zero)
        {
            this.Message("error.arrival");
            return;
        }
        Point arrival = new(anchor.X, anchor.Y + 2);
        this.Begin(new Journey(who, stable, horse, destination, arrival, quotedCost));
    }

    private void Begin(Journey trip)
    {
        if (!CanStart(trip.Who) || trip.Who.currentLocation != trip.Source || trip.Source != Game1.getFarm())
            return;
        if (HorseManager.FindStable(trip.Who) != trip.Stable
            || HorseManager.FindHorse(trip.Who, trip.Stable) != trip.Horse
            || !HorseManager.IsAvailable(trip.Who, trip.Horse))
        {
            this.Message("error.horse");
            return;
        }
        // Town festival maps can replace the street and queue multiplayer ready checks.
        if (Utility.isFestivalDay())
        {
            this.Message("error.festival");
            return;
        }
        if (trip.Who.Money < trip.Cost)
        {
            this.Message("error.money");
            return;
        }
        if (HorseManager.FindArrival(trip.Destination, trip.PreferredArrival, trip.Who, trip.Horse) == null)
        {
            this.monitor.Log($"No free horse arrival tile in {trip.Destination.NameOrUniqueName} near {trip.PreferredArrival}.", LogLevel.Debug);
            this.Message("error.arrival-blocked");
            return;
        }

        this.journey = trip;
        trip.Watch.Restart();
        // Move an unmounted horse onto the player's map BEFORE reserving its mutex.
        // NetMutex releases a lock when its owner isn't in the horse's location.
        if (trip.Horse.currentLocation != trip.Source)
        {
            if (Game1.IsClient)
            {
                if (this.helper.Multiplayer.GetConnectedPlayer(Game1.MasterPlayer.UniqueMultiplayerID)?.GetMod(ModEntry.ModId) == null)
                {
                    this.Cancel(false);
                    this.Message("error.host");
                    return;
                }
                trip.WaitingForHorse = true;
                this.helper.Multiplayer.SendMessage(new HorseTransferRequest { HorseId = trip.Horse.HorseId },
                    "BringHorse", new[] { ModEntry.ModId }, new[] { Game1.MasterPlayer.UniqueMultiplayerID });
                return;
            }
            Game1.warpCharacter(trip.Horse, trip.Source, trip.Who.Position / 64f);
        }
        this.AcquireHorse(trip);
    }

    private void AcquireHorse(Journey trip)
    {
        if (trip.Who.mount == trip.Horse)
            this.StartRide(trip);
        else
        {
            trip.Horse.mutex.RequestLock(() =>
            {
                // NetMutex callbacks can arrive after cancellation or a save/title change.
                if (this.journey != trip)
                {
                    trip.Horse.mutex.ReleaseLock();
                    return;
                }
                trip.OwnsNewLock = true;
                this.StartRide(trip);
            }, () =>
            {
                if (this.journey == trip)
                    this.Cancel(true);
            });
        }
    }

    private void StartRide(Journey trip)
    {
        try
        {
            if (!CanStart(trip.Who) || trip.Who.currentLocation != trip.Source
                || trip.Horse.rider != null && trip.Horse.rider != trip.Who
                || HorseManager.FindHorse(trip.Who, trip.Stable) != trip.Horse)
            {
                this.Cancel(true);
                return;
            }
            if (HorseManager.FindArrival(trip.Destination, trip.PreferredArrival, trip.Who, trip.Horse) == null
                || trip.Who.Money < trip.Cost)
            {
                this.Cancel(true);
                return;
            }

            HorseManager.Mount(trip.Who, trip.Horse);
            RidePath path = HorseManager.CreateRide(trip.Source, trip.Stable, trip.Horse);
            trip.Animation = new TravelAnimation(trip.Who, trip.Horse, path, this.config.AnimationDurationMs);
            trip.Who.controller = trip.Animation;
            trip.Who.Position = path.Start;
            trip.Who.faceDirection(path.Direction);
            trip.Horse.SyncPositionToRider();
            // Skips UpdateControlInput (which otherwise calls Farmer.Halt every frame),
            // while Farmer.Update and its native horse animations continue normally.
            trip.FrozeControls = true;
            Game1.freezeControls = true;
            if (!trip.Payment.TryReserve(trip.Who.Money, trip.Cost))
            {
                this.Cancel(true);
                return;
            }
            trip.Watch.Restart();
        }
        catch (Exception ex)
        {
            this.Fail(ex);
        }
    }

    public void BeforeUpdate()
    {
        if (!this.IsBusy)
            return;
        foreach (SButton button in Buttons)
        {
            if (this.helper.Input.IsDown(button))
                this.helper.Input.Suppress(button);
        }
        this.helper.Input.SuppressScrollWheel();
    }

    public void AfterUpdate()
    {
        try
        {
            if (this.afterMenu != null && Context.IsWorldReady && Game1.activeClickableMenu == null)
            {
                Action action = this.afterMenu;
                this.afterMenu = null;
                action();
            }
            Journey? trip = this.journey;
            if (trip == null)
                return;
            if (!Context.IsWorldReady || Game1.eventUp || Game1.CurrentEvent != null || Game1.farmEvent != null || Game1.HostPaused
                || Game1.activeClickableMenu != null || trip.Who.currentLocation != trip.Source && !trip.RequestedWarp)
            {
                this.Cancel(true);
                return;
            }
            if (trip.Animation == null)
            {
                if (trip.Watch.ElapsedMilliseconds > 5000)
                    this.Cancel(true);
                else if (trip.WaitingForHorse)
                {
                    Horse? synced = HorseManager.FindHorse(trip.Who, trip.Stable);
                    if (synced?.currentLocation == trip.Source && trip.Source.characters.Contains(synced))
                    {
                        trip.Horse = synced;
                        trip.WaitingForHorse = false;
                        trip.Watch.Restart();
                        this.AcquireHorse(trip);
                    }
                }
                return;
            }
            if (trip.Animation.Failure != null)
            {
                this.Fail(trip.Animation.Failure);
                return;
            }
            if (!trip.RequestedWarp && (trip.Who.controller != trip.Animation || trip.Who.mount != trip.Horse
                || !trip.Who.CanMove || Game1.isWarping || Game1.globalFade || Game1.fadeToBlackAlpha > 0
                || trip.Watch.ElapsedMilliseconds > this.config.AnimationDurationMs + 15000))
            {
                this.Cancel(true);
                return;
            }
            if (trip.RequestedWarp)
            {
                if (trip.Watch.ElapsedMilliseconds > 15000)
                    this.Cancel(true);
                return;
            }
            if (!trip.Animation.Finished)
                return;

            Point? arrival = HorseManager.FindArrival(trip.Destination, trip.PreferredArrival, trip.Who, trip.Horse);
            if (arrival == null || Utility.isFestivalDay())
            {
                this.Cancel(true);
                return;
            }
            trip.Animation.Stop();
            Game1.freezeControls = false;
            trip.FrozeControls = false;
            trip.RequestedWarp = true;
            trip.LockedControls = true;
            trip.Who.CanMove = false;
            trip.Watch.Restart();
            // This native warp owns its fade, location synchronization and outdoor mounted travel.
            Game1.warpFarmer(trip.Destination.NameOrUniqueName, arrival.Value.X, arrival.Value.Y, 2);
        }
        catch (Exception ex)
        {
            this.Fail(ex);
        }
    }

    public void OnWarped(GameLocation location)
    {
        Journey? trip = this.journey;
        if (trip == null)
            return;
        if (Game1.eventUp || Game1.CurrentEvent != null)
        {
            // An arrival event takes ownership of the player. Never reposition its actors.
            this.Cancel(false);
            return;
        }
        if (!trip.RequestedWarp || location != trip.Destination || trip.Who.mount != trip.Horse)
        {
            this.Cancel(true);
            return;
        }
        try
        {
            trip.Horse.currentLocation = location;
            trip.Horse.SyncPositionToRider();
            // Revalidate against the actual destination map after native reset/festival edits.
            Point? safe = HorseManager.FindArrival(location, trip.Who.TilePoint, trip.Who, trip.Horse);
            if (safe == null)
            {
                // Keep the mounted pair together; native warp back, refund and release our controls.
                Game1.warpFarmer(trip.Source.NameOrUniqueName, (int)(trip.Animation!.StartPosition.X / 64),
                    (int)(trip.Animation.StartPosition.Y / 64), 2);
                this.Cancel(true);
                return;
            }
            trip.Who.Position = new Vector2(safe.Value.X * 64, safe.Value.Y * 64);
            trip.Horse.SyncPositionToRider();
            trip.Payment.Commit();
            this.Release(trip);
            this.journey = null;
        }
        catch (Exception ex)
        {
            this.Fail(ex);
        }
    }

    public void Cancel(bool notify)
    {
        this.afterMenu = null;
        Journey? trip = this.journey;
        if (trip == null)
            return;
        this.journey = null;
        try
        {
            trip.Payment.Refund();
        }
        finally
        {
            this.Release(trip, restoreStart: !trip.RequestedWarp);
        }
        if (notify && Context.IsWorldReady && !Game1.eventUp && Game1.activeClickableMenu == null)
            this.Message("error.interrupted");
    }

    private void Release(Journey trip, bool restoreStart = false)
    {
        trip.Animation?.Stop(restoreStart);
        if (trip.FrozeControls && !Game1.eventUp && Game1.CurrentEvent == null)
            Game1.freezeControls = false;
        if (trip.LockedControls && !Game1.eventUp && Game1.CurrentEvent == null && !Game1.isWarping)
            trip.Who.CanMove = true;
        // A mounted horse keeps its native mutex until native dismount. Never release another rider's lock.
        if (trip.OwnsNewLock && trip.Who.mount != trip.Horse && trip.Horse.mutex.IsLockHeld())
            trip.Horse.mutex.ReleaseLock();
    }

    private void Message(string key) => Game1.showRedMessage(this.T(key));

    private void Fail(Exception ex)
    {
        this.monitor.Log($"Horse travel interrupted safely: {ex}", LogLevel.Error);
        this.Cancel(true);
    }
}
