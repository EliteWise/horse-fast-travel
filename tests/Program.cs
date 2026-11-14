using HorseFastTravel;
using Microsoft.Xna.Framework;

int assertions = 0;
void Check(bool condition, string message)
{
    assertions++;
    if (!condition)
        throw new InvalidOperationException(message);
}

// Interruption must neither mint money nor charge twice, including free trips.
foreach (int price in new[] { 0, 1, 100, 1000, 1_000_000 })
{
    int wallet = 1_000_000;
    TravelPayment payment = new(amount => wallet += amount);
    Check(payment.TryReserve(wallet, price), "A covered fare should be accepted.");
    Check(wallet == 1_000_000 - price, "The quoted fare must be debited exactly once.");
    Check(!payment.TryReserve(wallet, price), "Repeated start callbacks must not debit twice.");
    wallet -= 7; // Another purchase/shared-wallet update must survive a refund.
    payment.Refund();
    payment.Refund();
    Check(wallet == 999_993, "Cancellation must refund exactly once without overwriting other spending.");
}
{
    int wallet = 100;
    TravelPayment payment = new(amount => wallet += amount);
    Check(!payment.TryReserve(wallet, 101), "An unaffordable trip must not begin.");
    Check(!payment.TryReserve(wallet, -1), "A negative fare must not create money.");
    Check(wallet == 100, "Rejected trips must not change money.");
    Check(payment.TryReserve(wallet, 100), "An exact-balance fare should work.");
    payment.Commit();
    payment.Refund();
    Check(wallet == 0, "Successful travel must not be refunded by later cleanup.");
}

// Duration must change speed, not distance, endpoint, or in-game time-of-day.
foreach (int duration in new[] { 500, 2000, 10_000 })
foreach (Vector2 end in new[] { new Vector2(320, 0), new Vector2(-320, 0), new Vector2(0, 320), new Vector2(0, -320) })
{
    RidePath ride = new(Vector2.Zero, end, 1);
    Check(ride.PositionAt(-1, duration) == Vector2.Zero, "Progress must clamp before departure.");
    Check(ride.PositionAt(duration / 2d, duration) == end / 2, "The rider must visibly move before the fade.");
    Check(ride.PositionAt(duration, duration) == end, "The whole configured ride must finish.");
    Check(ride.PositionAt(duration * 2, duration) == end, "A stalled tick must not overshoot.");
}

// Scenery must not prevent the two-second cinematic, but the horse must stay on-map.
foreach (int startX in new[] { 0, 260, 520 })
foreach (int startY in new[] { 0, 200, 448 })
{
    Vector2 start = new(startX, startY);
    Rectangle footprint = new(startX + 8, startY + 16, 96, 32);
    RidePath cinematic = RidePath.CreateCinematic(start, footprint, 640, 512);
    Check(Vector2.Distance(cinematic.Start, cinematic.End) >= 128, "The cinematic must show real movement near every map edge.");
    for (int frame = 0; frame <= 120; frame++)
    {
        Vector2 offset = cinematic.PositionAt(frame * 2000d / 120, 2000) - start;
        Rectangle moved = footprint;
        moved.Offset((int)offset.X, (int)offset.Y);
        Check(moved.Left >= 0 && moved.Top >= 0 && moved.Right < 640 && moved.Bottom < 512,
            "Ignoring scenery must never move the horse outside the map.");
    }
}
{
    RidePath cinematic = RidePath.CreateCinematic(Vector2.Zero, new Rectangle(8, 16, 96, 32), 640, 512);
    Rectangle fence = new(128, 0, 96, 64);
    Check(fence.Contains(cinematic.PositionAt(1000, 2000)), "The cinematic must accept crossing a departure obstacle.");
    Check(cinematic.PositionAt(2000, 2000) == cinematic.End, "The blocked departure must still finish after two seconds.");
}

ModConfig config = new() { TravelCost = int.MinValue, AnimationDurationMs = int.MaxValue };
Check(config.Normalize(), "Invalid config should be normalized.");
Check(config.TravelCost == 0 && config.AnimationDurationMs == 10000, "Unsafe config values must be clamped.");
Check(!config.Normalize(), "Config normalization should be stable.");
config = new ModConfig();
Check(config.TravelCost == 1000 && config.AnimationDurationMs == 2000,
    "Default configuration must match the specification.");
Console.WriteLine($"PASS: {assertions} assertions (payment, interruption, cinematic obstacles/map bounds, duration, configuration).");
