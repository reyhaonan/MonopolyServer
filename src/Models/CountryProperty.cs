public class CountryProperty : Property
{
    public ColorGroup Group { get; set; }
    public decimal[] RentScheme { get; set; } // [Unimproved, 1H, 2H, 3H, 4H, Hotel]
    public decimal HouseCost { get; set; }
    public int NumHouses { get; set; } // 0-4
    public bool HasHotel { get; set; } // True if 1 hotel (replaces 4 houses)

    public CountryProperty(string name, int boardPosition, decimal price, ColorGroup group, decimal[] rentScheme, decimal houseCost)
        : base(name, boardPosition, price)
    {
        Group = group;
        RentScheme = rentScheme;
        HouseCost = houseCost;
        NumHouses = 0;
        HasHotel = false;
    }

    public override decimal CalculateRent(int diceRoll = 0, int ownerRailroads = 0, int ownerUtilities = 0)
    {
        if (IsMortgaged) return 0;

        // Rent scheme is [Unimproved, 1H, 2H, 3H, 4H, Hotel]
        // Index 0 for unimproved, 1-4 for houses, 5 for hotel
        if (HasHotel) return RentScheme[5];
        return RentScheme[NumHouses];
    }
}