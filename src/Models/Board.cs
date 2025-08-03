using MonopolyServer.Enums;

namespace MonopolyServer.Models;

public class Board
{
    public List<Space> Spaces { get; private set; } = [];

    public Board()
    {
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        Spaces.Add(new SpecialSpace("GO!", 0, SpecialSpaceType.Go));
        Spaces.Add(new CountryProperty("Bhutan", 1, 60, ColorGroup.Brown, [2, 10, 30, 90, 160, 250], 50));
        Spaces.Add(new SpecialSpace("Community Chest", 2, SpecialSpaceType.CommunityChest));
        Spaces.Add(new CountryProperty("Laos", 3, 60, ColorGroup.Brown, [4, 20, 60, 180, 320, 450], 50));
        Spaces.Add(new SpecialSpace("Income Tax", 4, SpecialSpaceType.IncomeTax));
        Spaces.Add(new RailroadProperty("Shibuya Station", 5));
        Spaces.Add(new CountryProperty("Cambodia", 6, 100, ColorGroup.LightBlue, [6, 30, 90, 270, 400, 550], 50));
        Spaces.Add(new SpecialSpace("Chance", 7, SpecialSpaceType.Chance));
        Spaces.Add(new CountryProperty("Vietnam", 8, 100, ColorGroup.LightBlue, [6, 30, 90, 270, 400, 550], 50));
        Spaces.Add(new CountryProperty("Malaysia", 9, 120, ColorGroup.LightBlue, [8, 40, 100, 300, 450, 600], 50));
        Spaces.Add(new SpecialSpace("Jail / Just Visiting", 10, SpecialSpaceType.Jail)); // Jail space
        Spaces.Add(new CountryProperty("Portugal", 11, 140, ColorGroup.Pink, [10, 50, 150, 450, 625, 750], 100));
        Spaces.Add(new UtilityProperty("Electric Company", 12));
        Spaces.Add(new CountryProperty("Greece", 13, 140, ColorGroup.Pink, [10, 50, 150, 450, 625, 750], 100));
        Spaces.Add(new CountryProperty("Ireland", 14, 160, ColorGroup.Pink, [12, 60, 180, 500, 700, 900], 100));
        Spaces.Add(new RailroadProperty("Changi Airport", 15));
        Spaces.Add(new CountryProperty("Poland", 16, 180, ColorGroup.Orange, [14, 70, 200, 550, 750, 950], 100));
        Spaces.Add(new SpecialSpace("Community Chest", 17, SpecialSpaceType.CommunityChest));
        Spaces.Add(new CountryProperty("Slovakia", 18, 180, ColorGroup.Orange, [14, 70, 200, 550, 750, 950], 100));
        Spaces.Add(new CountryProperty("Hungary", 19, 200, ColorGroup.Orange, [16, 80, 220, 600, 800, 1000], 100));
        Spaces.Add(new SpecialSpace("Free Parking", 20, SpecialSpaceType.FreeParking));
        Spaces.Add(new CountryProperty("Brazil", 21, 220, ColorGroup.Red, [18, 90, 250, 700, 875, 1050], 150));
        Spaces.Add(new SpecialSpace("Chance", 22, SpecialSpaceType.Chance));
        Spaces.Add(new CountryProperty("Argentina", 23, 220, ColorGroup.Red, [18, 90, 250, 700, 875, 1050], 150));
        Spaces.Add(new CountryProperty("Chile", 24, 240, ColorGroup.Red, [20, 100, 300, 750, 925, 1100], 150));
        Spaces.Add(new RailroadProperty("London Station", 25));
        Spaces.Add(new CountryProperty("Spain", 26, 260, ColorGroup.Yellow, [22, 110, 330, 800, 975, 1150], 150));
        Spaces.Add(new CountryProperty("Italy", 27, 260, ColorGroup.Yellow, [22, 110, 330, 800, 975, 1150], 150));
        Spaces.Add(new UtilityProperty("Water Company", 28));
        Spaces.Add(new CountryProperty("Australia", 29, 280, ColorGroup.Yellow, [24, 120, 360, 850, 1025, 1200], 150));
        Spaces.Add(new SpecialSpace("Go To Jail", 30, SpecialSpaceType.GoToJail));
        Spaces.Add(new CountryProperty("Canada", 31, 300, ColorGroup.Green, [26, 130, 390, 900, 1100, 1275], 200));
        Spaces.Add(new CountryProperty("Germany", 32, 300, ColorGroup.Green, [26, 130, 390, 900, 1100, 1275], 200));
        Spaces.Add(new SpecialSpace("Community Chest", 33, SpecialSpaceType.CommunityChest));
        Spaces.Add(new CountryProperty("France", 34, 320, ColorGroup.Green, [28, 150, 450, 1000, 1200, 1400], 200));
        Spaces.Add(new RailroadProperty("JFK Airport", 35));
        Spaces.Add(new SpecialSpace("Chance", 36, SpecialSpaceType.Chance));
        Spaces.Add(new CountryProperty("United States", 37, 350, ColorGroup.DarkBlue, [35, 175, 500, 1100, 1300, 1500], 200));
        Spaces.Add(new SpecialSpace("Luxury Tax", 38, SpecialSpaceType.LuxuryTax));
        Spaces.Add(new CountryProperty("China", 39, 400, ColorGroup.DarkBlue, [50, 200, 600, 1400, 1700, 2000], 200));
    }

    public Property GetPropertyById(Guid propertyId)
    {
        var property = Spaces.OfType<Property>().FirstOrDefault(p => p.Id == propertyId) ?? throw new Exception("Property not found");

        return property;
    }

    public List<Property> GetPropertiesByGuidList(List<Guid> propertiesId)
    {
        return Spaces.OfType<Property>().Where(property => propertiesId.Contains(property.Id)).ToList();
    }

    public List<CountryProperty> GetPropertiesInGroup(ColorGroup group)
    {
        return Spaces.OfType<CountryProperty>().Where(p => p.Group == group).ToList();
    }
    public List<UtilityProperty> GetUtilityOwnedByPlayer(Guid playerId)
    {
        return Spaces.OfType<UtilityProperty>().Where(p => p.OwnerId == playerId).ToList();
    }
    public List<RailroadProperty> GetRailroadOwnedByPlayer(Guid playerId)
    {
        return Spaces.OfType<RailroadProperty>().Where(p => p.OwnerId == playerId).ToList();
    }

    public bool GroupIsOwnedByPlayer(ColorGroup group, Guid playerId)
    {
        var properties = GetPropertiesInGroup(group);
        return properties.All(property => property.OwnerId != null && property.OwnerId.Equals(playerId));
    }
    public bool NoMortgagedPropertyInGroup(ColorGroup group)
    {
        var properties = GetPropertiesInGroup(group);
        return properties.All(property => property.OwnerId != null && !property.IsMortgaged);
    }
}

