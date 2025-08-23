public record GameConfig
{
    public int MaxPlayers { get; init; } = 8;
    public int MinPlayers { get; init; } = 2;
    public int JailFine { get; init; } = 50;// yes
    public bool FreeParkingPot { get; set; } = false; 
    public bool DoubleBaseRentOnFullColorSet { get; set; } = false; 
    public bool AllowCollectRentOnJail { get; set; } = true; 
    public bool AllowMortgagingProperties { get; set; } = true; 
    public bool BalancedHousePurchase { get; set; } = true; 
    public int StartingMoney { get; set; } = 1500;

    // This one kind of complex ima do it last
    public bool AuctionOnNoPurchase { get; set; } = false;
}