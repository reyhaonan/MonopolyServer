using System.Text.Json.Serialization;
using MonopolyServer.Enums;

namespace MonopolyServer.Models;
public class CountryProperty : Property
{
    [JsonInclude]
    public ColorGroup Group { get; init; }
    [JsonInclude]
    public int[] RentScheme { get; init; } // [Unimproved, 1H, 2H, 3H, 4H, Hotel]
    [JsonInclude]
    public int HouseCost { get; init; }
    [JsonInclude]
    public int HouseSellValue { get; init; } // 1/2 of HouseCost
    [JsonInclude]
    public RentStage CurrentRentStage { get; private set; } // 0-4

    public CountryProperty(string name, int boardPosition, int price, ColorGroup group, int[] rentScheme, int houseCost)
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

    public override int CalculateRent(bool doubleBaseRent = false)
    {
        if (IsMortgaged) return 0;
        if (doubleBaseRent && CurrentRentStage == RentStage.Unimproved) return RentScheme[(int)CurrentRentStage] * 2;
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