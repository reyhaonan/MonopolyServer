
using System.Text.Json.Serialization;
namespace MonopolyServer.Models;

public class Property : Space
{
    [JsonInclude]
    public decimal PurchasePrice { get; init; }
    // This is also the sell price, if property is mortgaged, then sold. Player wont get any money
    [JsonInclude]
    public decimal MortgageValue { get; init; } // 50% of purchase price
    [JsonInclude]
    public decimal UnmortgageCost { get; init; } // 60% of purchase price
    [JsonInclude]
    public Guid? OwnerId { get; private set; }
    [JsonInclude]
    public bool IsMortgaged { get; private set; }

    protected Property(string name, int boardPosition, decimal price)
        : base(name, boardPosition)
    {
        PurchasePrice = price;
        MortgageValue = price / 2;
        UnmortgageCost = price * .6m;
        OwnerId = null;
        IsMortgaged = false;
    }

    public virtual decimal CalculateRent(int diceRoll = 0, int ownerRailroads = 0, int ownerUtilities = 0) { return 9999; }

    public virtual void ResetProperty()
    {
        OwnerId = null;
        IsMortgaged = false;
    }

    // <summary>
    // Return false if owner is null
    // </summary>
    public bool IsOwnedByOtherPlayer(Guid playerGuid)
    {
        if (OwnerId == null) return false;
        return !OwnerId.Equals(playerGuid);
    }

    public bool IsOwnedByPlayer(Guid playerGuid)
    {
        if (OwnerId == null) return false;
        return OwnerId.Equals(playerGuid);
    }

    public void BuyProperty(Guid playerGuid)
    {
        OwnerId = playerGuid;
    }

    public virtual void MortgageProperty()
    {
        if (OwnerId == null) throw new Exception("Nobody own this...");
        IsMortgaged = true;
    }

    public virtual void UnmortgageProperty()
    {
        if (OwnerId == null) throw new Exception("Nobody own this...");
        IsMortgaged = false;
    }

    public virtual void SellProperty()
    {
        if (OwnerId == null) throw new Exception("Nobody own this...");
        if (IsMortgaged) throw new InvalidOperationException("Cannot sell mortgaged property");
        ResetProperty();
    }
}
