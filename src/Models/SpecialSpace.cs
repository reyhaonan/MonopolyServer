
using MonopolyServer.Enums;

namespace MonopolyServer.Models;
public class SpecialSpace : Space
{
    public SpecialSpaceType Type { get; set; } // e.g., Go, Jail, Chance, Tax

    public SpecialSpace(string name, int boardPosition, SpecialSpaceType type)
        : base(name, boardPosition)
    {
        Type = type;
    }
}
