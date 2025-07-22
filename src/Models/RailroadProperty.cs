public class RailroadProperty : Property
{
    public RailroadProperty(string name, int boardPosition)
        : base(name, boardPosition, 200) // All railroads cost $200
    {
    }

    public override decimal CalculateRent(int diceRoll = 0, int ownerRailroads = 0, int ownerUtilities = 0)
    {
        if (IsMortgaged) return 0;

        switch (ownerRailroads)
        {
            case 1: return 25;
            case 2: return 50;
            case 3: return 100;
            case 4: return 200;
            default: return 0; // Should not happen if logic is correct
        }
    }
}
