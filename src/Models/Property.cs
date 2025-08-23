
using System.Text.Json.Serialization;
namespace MonopolyServer.Models;

public class Property : Space
{
    [JsonInclude]
    public int PurchasePrice { get; init; }
    // This is also the sell price, if property is mortgaged, then sold. Player wont get any money
    [JsonInclude]
    public int MortgageValue { get; init; } // 50% of purchase price
    [JsonInclude]
    public int UnmortgageCost { get; init; } // 60% of purchase price
    [JsonInclude]
    public Guid? OwnerId { get; private set; }
    [JsonInclude]
    public bool IsMortgaged { get; private set; }

    protected Property(string name, int boardPosition, int price)
        : base(name, boardPosition)
    {
        PurchasePrice = price;
        MortgageValue = price / 2;
        UnmortgageCost = (int)Math.Floor(price * .6m);
        OwnerId = null;
        IsMortgaged = false;
    }

    public virtual int CalculateRent(int diceRoll = 0, int ownerRailroads = 0, int ownerUtilities = 0) { return 9999; }

    public virtual void ResetProperty()
    {
        OwnerId = null;
        IsMortgaged = false;
    }

    // <summary>
    // Return false if owner is null
    // </summary>
    public bool IsOwnedByOtherPlayer(Guid playerId)
    {
        if (OwnerId == null) return false;
        return !OwnerId.Equals(playerId);
    }

    public bool IsOwnedByPlayer(Guid playerId)
    {
        if (OwnerId == null) return false;
        return OwnerId.Equals(playerId);
    }

    public void BuyProperty(Guid playerId)
    {
        ChangeOwner(playerId);
    }

    public void MortgageProperty()
    {
        IsMortgaged = true;
    }

    public void UnmortgageProperty()
    {
        IsMortgaged = false;
    }

    public void SellProperty()
    {
        ResetProperty();
    }

    public void ChangeOwner(Guid newOwnerId)
    {
        OwnerId = newOwnerId;
    }
}
