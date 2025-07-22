public record GameConfig
{
    public int MaxPlayers { get; init; } = 8;
    public int MinPlayers { get; init; } = 2;
    public int JailFine { get; init; } = 50;
    public bool AuctionOnNoPurchase { get; init; } = true;
    public bool FreeParkingPot { get; init; } = true;
    public bool DoubleBaseRentOnFullColorSet { get; init; } = true;
    public bool AllowCollectRentOnJail { get; init; } = true;

    public bool AllowMortgagingProperties { get; init; } = true;
    public int StartingMoney { get; init; } = 1500;
    public bool BalancedHousePurchase { get; init; } = true;

}