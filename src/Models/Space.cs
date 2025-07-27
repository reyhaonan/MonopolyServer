using System.Text.Json.Serialization;

namespace MonopolyServer.Models;
[JsonDerivedType(typeof(SpecialSpace), typeDiscriminator: "special")]
[JsonDerivedType(typeof(Property), typeDiscriminator: "property")]
[JsonDerivedType(typeof(RailroadProperty), typeDiscriminator: "railroad")]
[JsonDerivedType(typeof(CountryProperty), typeDiscriminator: "country")]
[JsonDerivedType(typeof(UtilityProperty), typeDiscriminator: "utility")]
public class Space
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int BoardPosition { get; set; } // 0 to 39

    protected Space(string name, int boardPosition)
    {
        Id = Guid.NewGuid();
        Name = name;
        BoardPosition = boardPosition;
    }
}
