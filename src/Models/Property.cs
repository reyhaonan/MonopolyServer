
public abstract class Property : Space
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

    public abstract decimal CalculateRent(int diceRoll = 0, int ownerRailroads = 0, int ownerUtilities = 0);
}
