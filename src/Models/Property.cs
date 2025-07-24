
public class Property : Space
{
    public decimal PurchasePrice { get; set; }
    public decimal MortgageValue { get; set; } // 50% of purchase price
    public Guid? OwnerId { get; set; }
    public bool IsMortgaged { get; set; }

    protected Property(string name, int boardPosition, decimal price)
        : base(name, boardPosition)
    {
        PurchasePrice = price;
        MortgageValue = price / 2;
        OwnerId = null;
        IsMortgaged = false;
    }

    public virtual decimal CalculateRent(int diceRoll = 0, int ownerRailroads = 0, int ownerUtilities = 0) { return 9999; }

    // <summary>
    // Return false if owner is null
    // </summary>
    public bool IsOwnedByOtherPlayer(Guid playerGuid)
    {
        if (OwnerId == null) return false;
        return !OwnerId.Equals(playerGuid);
    }
}
