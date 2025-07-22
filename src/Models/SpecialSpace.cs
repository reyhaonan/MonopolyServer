
public class SpecialSpace : Space
{
    public PropertyType Type { get; set; } // e.g., Go, Jail, Chance, Tax

    public SpecialSpace(string name, int boardPosition, PropertyType type)
        : base(name, boardPosition)
    {
        Type = type;
    }
}
