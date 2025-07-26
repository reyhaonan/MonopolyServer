using System.Text.Json.Serialization;
using Confluent.Kafka;

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

    protected override void ResetProperty()
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
        if (OwnerId == null || CurrentRentStage == RentStage.Hotel) throw new InvalidOperationException("Cannot upgrade more in this property");

        if (IsMortgaged) throw new InvalidOperationException("Cannot upgrade mortgaged property");
        CurrentRentStage++;
    }

    public void DownGradeRentStage()
    {
        if (OwnerId == null || CurrentRentStage == RentStage.Unimproved) throw new InvalidOperationException("Cannot downgrade more in this property");
        
        if (IsMortgaged) throw new InvalidOperationException("Cannot upgrade mortgaged property");
        CurrentRentStage--;

    }

    public override void MortgageProperty()
    {
        if (CurrentRentStage != RentStage.Unimproved) throw new InvalidOperationException("Can't mortgage property with house");
        base.MortgageProperty();
    }

    public override void SellProperty()
    {
        if (CurrentRentStage != RentStage.Unimproved) throw new InvalidOperationException("Can't sell property with house");
        if(IsMortgaged) throw new InvalidOperationException("Can't sell mortgaged property");
        base.SellProperty();
    }
}