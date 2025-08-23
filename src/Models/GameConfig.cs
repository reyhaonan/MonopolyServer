public record GameConfig
{
    public int MaxPlayers { get; init; } = 8;//yes
    public int MinPlayers { get; init; } = 2;//yes
    public int JailFine { get; init; } = 50;// yes
    public bool AuctionOnNoPurchase { get; set; } = false;
    public bool FreeParkingPot { get; set; } = false;
    public bool DoubleBaseRentOnFullColorSet { get; set; } = false;
    public bool AllowCollectRentOnJail { get; set; } = false; //yes
    public bool AllowMortgagingProperties { get; set; } = false; //yes
    public bool BalancedHousePurchase { get; set; } = true; //yes
    public int StartingMoney { get; set; } = 1500; // yes

}