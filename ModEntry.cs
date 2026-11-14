using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace HorseFastTravel;

public sealed class ModEntry : Mod
{
    internal const string ModId = "EliteWise.HorseFastTravel";
    private readonly PerScreen<TravelManager?> travel = new();
    private ModConfig config = null!;

    public override void Entry(IModHelper helper)
    {
        this.config = helper.ReadConfig<ModConfig>();
        if (this.config.Normalize())
            this.Monitor.Log("TravelCost must be 0–1000000; AnimationDurationMs must be 500–10000. Invalid values were clamped.", LogLevel.Warn);
        helper.WriteConfig(this.config);

        helper.Events.GameLoop.SaveLoaded += (_, _) => this.travel.Value = new TravelManager(helper, this.Monitor, this.config);
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.UpdateTicking += (_, _) => this.travel.Value?.BeforeUpdate();
        helper.Events.GameLoop.UpdateTicked += (_, _) => this.travel.Value?.AfterUpdate();
        helper.Events.Player.Warped += (_, e) => { if (e.IsLocalPlayer) this.travel.Value?.OnWarped(e.NewLocation); };
        helper.Events.Multiplayer.ModMessageReceived += this.OnModMessage;
        helper.Events.GameLoop.Saving += (_, _) => this.travel.Value?.Cancel(false);
        helper.Events.GameLoop.DayEnding += (_, _) => this.travel.Value?.Cancel(false);
        helper.Events.GameLoop.ReturnedToTitle += (_, _) =>
        {
            this.travel.Value?.Cancel(false);
            this.travel.Value = null;
        };
    }

    private void OnModMessage(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || e.FromModID != ModId || e.Type != "BringHorse")
            return;
        try
        {
            Farmer? who = Game1.GetPlayer(e.FromPlayerID);
            if (who == null || who.mount != null || who.currentLocation == null)
                return;
            Stable? stable = HorseManager.FindStable(who);
            if (stable == null || stable.HorseId != e.ReadAs<HorseTransferRequest>().HorseId)
                return;
            Horse? horse = HorseManager.FindHorse(who, stable);
            if (horse == null || !HorseManager.IsAvailable(who, horse)
                || who.currentLocation != Game1.getFarm())
                return;
            // Validate the actual sender's horse and position. Never trust client coordinates.
            Game1.warpCharacter(horse, who.currentLocation, who.Position / 64f);
        }
        catch (System.Exception ex)
        {
            this.Monitor.Log($"Could not bring a farmhand's horse: {ex}", LogLevel.Warn);
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        TravelManager? manager = this.travel.Value;
        if (manager == null || !Context.IsWorldReady)
            return;

        if (manager.IsBusy)
        {
            this.Helper.Input.Suppress(e.Button);
            if (e.Button is SButton.Escape or SButton.ControllerB)
                manager.Cancel(true);
            return;
        }

        if (!e.IsSuppressed() && e.Button.IsActionButton() && Context.CanPlayerMove
            && manager.TryOpenMenu(e.Cursor.GrabTile))
            this.Helper.Input.Suppress(e.Button);
    }
}
