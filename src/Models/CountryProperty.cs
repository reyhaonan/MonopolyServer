using System.Text.Json.Serialization;
using MonopolyServer.Enums;

namespace MonopolyServer.Models;
public class CountryProperty : Property
{
    [JsonInclude]
    public ColorGroup Group { get; init; }
    [JsonInclude]
    public decimal[] RentScheme { get; init; } // [Unimproved, 1H, 2H, 3H, 4H, Hotel]
    [JsonInclude]
    public decimal HouseCost { get; init; }
    [JsonInclude]
    public decimal HouseSellValue { get; init; } // 1/2 of HouseCost
    [JsonInclude]
    public RentStage CurrentRentStage { get; private set; } // 0-4

    public CountryProperty(string name, int boardPosition, decimal price, ColorGroup group, decimal[] rentScheme, decimal houseCost)
        : base(name, boardPosition, price)
    {
        Group = group;
        RentScheme = rentScheme;
        HouseCost = houseCost;
        HouseSellValue = houseCost / 2;
        CurrentRentStage = RentStage.Unimproved;
    }

    public override void ResetProperty()
    {
        CurrentRentStage = RentStage.Unimproved;
        base.ResetProperty();
    }

    public override decimal CalculateRent(int diceRoll = 0, int ownerRailroads = 0, int ownerUtilities = 0)
    {
        if (IsMortgaged) return 0;

        return RentScheme[(int)CurrentRentStage];
    }

    public void UpgradeRentStage()
    {
        CurrentRentStage++;
    }

    public void DownGradeRentStage()
    {
        CurrentRentStage--;

    }

}