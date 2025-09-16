public class TreasureCard
{
    public TreasureOutcome TreasureOutcome { get; init; }
    public int MoveAdded { get; init; }
    public int MonetaryAmount { get; init; }
    public int PropertyDestination { get; init; }
    public required string FlavorText { get; init; }
}