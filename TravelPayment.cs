using System;

namespace HorseFastTravel;

/// <summary>One debit and, if interrupted, exactly one refund of the quoted amount.</summary>
internal sealed class TravelPayment
{
    private readonly Action<int> changeMoney;
    private int reserved;
    private bool started;

    public TravelPayment(Action<int> changeMoney) => this.changeMoney = changeMoney;

    public bool TryReserve(int balance, int cost)
    {
        if (cost < 0 || cost > balance || this.started)
            return false;
        this.changeMoney(-cost);
        this.reserved = cost;
        this.started = true;
        return true;
    }

    public void Commit() => this.reserved = 0;

    public void Refund()
    {
        int amount = this.reserved;
        this.reserved = 0;
        if (amount != 0)
            this.changeMoney(amount);
    }
}
