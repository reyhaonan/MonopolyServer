namespace MonopolyServer.Models;
public class UtilityProperty : Property
{
    public UtilityProperty(string name, int boardPosition)
        : base(name, boardPosition, 150) // All utilities cost $150
    {
    }

    public override int CalculateRent(int diceRoll = 0, int ownerRailroads = 0, int ownerUtilities = 0)
    {
        if (IsMortgaged) return 0;

        // Rent for utilities: 4x dice roll (1 utility), 10x dice roll (2 utilities)
        if (ownerUtilities == 1) return 4 * diceRoll;
        if (ownerUtilities == 2) return 10 * diceRoll;
        return 0; // Should not happen
    }
}
