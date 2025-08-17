
using System.Text.Json.Serialization;
using MonopolyServer.Enums;
using MonopolyServer.Models;

namespace MonopolyServer.Utils;
public readonly struct RollResult
{
    // Inner struct for dice roll details
    [method: JsonConstructor]    // Inner struct for dice roll details
    public readonly struct DiceInfo
    {
        public int Roll1 { get; init;}
        public int Roll2 { get; init;}
        public int TotalRoll { get; init;}
    }

    // Inner struct for player state after the roll
    [method: JsonConstructor]
    // Inner struct for player state after the roll
    public readonly struct PlayerStateInfo
    {
        public bool IsInJail { get;init; }
        public int NewPlayerPosition { get;init; }
        public int NewPlayerJailTurnsRemaining { get;init; }
        public int ConsecutiveDoubles { get;init; }
    }


    [JsonInclude]
    public DiceInfo Dice { get;init; }
    [JsonInclude]
    public PlayerStateInfo PlayerState { get;init; }
    [JsonInclude]
    public List<TransactionInfo> Transaction { get;init; }
    [JsonInclude]
    public GamePhase NewGamePhase { get;init; }

}