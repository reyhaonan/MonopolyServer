public class Board
{
    public List<Space> Spaces { get; set; }

    public Board()
    {
        Spaces = new List<Space>();
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        Spaces.Add(new SpecialSpace("GO!", 0, PropertyType.Go));
        Spaces.Add(new CountryProperty("Bhutan", 1, 60, ColorGroup.Brown, new decimal[] { 2, 10, 30, 90, 160, 250 }, 50));
        Spaces.Add(new SpecialSpace("Community Chest", 2, PropertyType.CommunityChest));
        Spaces.Add(new CountryProperty("Laos", 3, 60, ColorGroup.Brown, new decimal[] { 4, 20, 60, 180, 320, 450 }, 50));
        Spaces.Add(new SpecialSpace("Income Tax", 4, PropertyType.IncomeTax));
        Spaces.Add(new RailroadProperty("Japan", 5));
        Spaces.Add(new CountryProperty("Cambodia", 6, 100, ColorGroup.LightBlue, new decimal[] { 6, 30, 90, 270, 400, 550 }, 50));
        Spaces.Add(new SpecialSpace("Chance", 7, PropertyType.Chance));
        Spaces.Add(new CountryProperty("Vietnam", 8, 100, ColorGroup.LightBlue, new decimal[] { 6, 30, 90, 270, 400, 550 }, 50));
        Spaces.Add(new CountryProperty("Malaysia", 9, 120, ColorGroup.LightBlue, new decimal[] { 8, 40, 100, 300, 450, 600 }, 50));
        Spaces.Add(new SpecialSpace("Jail / Just Visiting", 10, PropertyType.Jail)); // Jail space
        Spaces.Add(new CountryProperty("Portugal", 11, 140, ColorGroup.Pink, new decimal[] { 10, 50, 150, 450, 625, 750 }, 100));
        Spaces.Add(new UtilityProperty("New Zealand", 12));
        Spaces.Add(new CountryProperty("Greece", 13, 140, ColorGroup.Pink, new decimal[] { 10, 50, 150, 450, 625, 750 }, 100));
        Spaces.Add(new CountryProperty("Ireland", 14, 160, ColorGroup.Pink, new decimal[] { 12, 60, 180, 500, 700, 900 }, 100));
        Spaces.Add(new RailroadProperty("India", 15));
        Spaces.Add(new CountryProperty("Poland", 16, 180, ColorGroup.Orange, new decimal[] { 14, 70, 200, 550, 750, 950 }, 100));
        Spaces.Add(new SpecialSpace("Community Chest", 17, PropertyType.CommunityChest));
        Spaces.Add(new CountryProperty("Slovakia", 18, 180, ColorGroup.Orange, new decimal[] { 14, 70, 200, 550, 750, 950 }, 100));
        Spaces.Add(new CountryProperty("Hungary", 19, 200, ColorGroup.Orange, new decimal[] { 16, 80, 220, 600, 800, 1000 }, 100));
        Spaces.Add(new SpecialSpace("Free Parking", 20, PropertyType.FreeParking));
        Spaces.Add(new CountryProperty("Brazil", 21, 220, ColorGroup.Red, new decimal[] { 18, 90, 250, 700, 875, 1050 }, 150));
        Spaces.Add(new SpecialSpace("Chance", 22, PropertyType.Chance));
        Spaces.Add(new CountryProperty("Argentina", 23, 220, ColorGroup.Red, new decimal[] { 18, 90, 250, 700, 875, 1050 }, 150));
        Spaces.Add(new CountryProperty("Chile", 24, 240, ColorGroup.Red, new decimal[] { 20, 100, 300, 750, 925, 1100 }, 150));
        Spaces.Add(new RailroadProperty("United Kingdom", 25));
        Spaces.Add(new CountryProperty("Spain", 26, 260, ColorGroup.Yellow, new decimal[] { 22, 110, 330, 800, 975, 1150 }, 150));
        Spaces.Add(new CountryProperty("Italy", 27, 260, ColorGroup.Yellow, new decimal[] { 22, 110, 330, 800, 975, 1150 }, 150));
        Spaces.Add(new UtilityProperty("Norway", 28));
        Spaces.Add(new CountryProperty("Australia", 29, 280, ColorGroup.Yellow, new decimal[] { 24, 120, 360, 850, 1025, 1200 }, 150));
        Spaces.Add(new SpecialSpace("Go To Jail", 30, PropertyType.GoToJail));
        Spaces.Add(new CountryProperty("Canada", 31, 300, ColorGroup.Green, new decimal[] { 26, 130, 390, 900, 1100, 1275 }, 200));
        Spaces.Add(new CountryProperty("Germany", 32, 300, ColorGroup.Green, new decimal[] { 26, 130, 390, 900, 1100, 1275 }, 200));
        Spaces.Add(new SpecialSpace("Community Chest", 33, PropertyType.CommunityChest));
        Spaces.Add(new CountryProperty("France", 34, 320, ColorGroup.Green, new decimal[] { 28, 150, 450, 1000, 1200, 1400 }, 200));
        Spaces.Add(new RailroadProperty("Egypt", 35));
        Spaces.Add(new SpecialSpace("Chance", 36, PropertyType.Chance));
        Spaces.Add(new CountryProperty("United States", 37, 350, ColorGroup.DarkBlue, new decimal[] { 35, 175, 500, 1100, 1300, 1500 }, 200));
        Spaces.Add(new SpecialSpace("Luxury Tax", 38, PropertyType.LuxuryTax));
        Spaces.Add(new CountryProperty("China", 39, 400, ColorGroup.DarkBlue, new decimal[] { 50, 200, 600, 1400, 1700, 2000 }, 200));
    }

    public Property? GetPropertyById(Guid propertyId)
    {
        return Spaces.OfType<Property>().FirstOrDefault(p => p.Id == propertyId);
    }

    public List<CountryProperty> GetPropertiesInGroup(ColorGroup group)
    {
        return Spaces.OfType<CountryProperty>().Where(p => p.Group == group).ToList();
    }
}

