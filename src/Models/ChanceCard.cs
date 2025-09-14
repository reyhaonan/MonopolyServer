public class ChanceCard
{
    public ChanceOutcome ChanceOutcome { get; init; }
    public int MoveAdded { get; init; }
    public int MonetaryAmount { get; init; }
    public int PropertyDestination { get; init; }
    public required string FlavorText { get; init; }
}